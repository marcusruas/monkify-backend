using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Context;
using Serilog;
using Monkify.Domain.Configs.ValueObjects;

namespace Monkify.Infrastructure.Services.Sessions
{
    public class SessionService : ISessionService
    {
        public SessionService(GeneralSettings settings, MonkifyDbContext context, IHubContext<ActiveSessionsHub> activeSessionsHub, SessionBetsTracker sessionBetsTracker)
        {
            _settings = settings;
            _context = context;
            _activeSessionsHub = activeSessionsHub;

            _sessionTracker = sessionBetsTracker;
        }

        private readonly GeneralSettings _settings;
        private readonly MonkifyDbContext _context;
        private readonly IHubContext<ActiveSessionsHub> _activeSessionsHub;
        private readonly SessionBetsTracker _sessionTracker;

        public async Task<MonkifyTyper> RunSession(Session session, CancellationToken cancellationToken)
        {
            var monkey = new MonkifyTyper(session);
            string terminalEndpoint = string.Format(_settings.Sessions.SessionTerminalEndpoint, monkey.SessionId.ToString());

            char[] batch = new char[_settings.Sessions.TerminalBatchLimit];
            int batchIndex = 0;

            while (!monkey.HasWinners && !cancellationToken.IsCancellationRequested)
            {
                batch[batchIndex++] = await monkey.GenerateNextCharacter(cancellationToken);
                if (batchIndex >= _settings.Sessions.TerminalBatchLimit)
                {
                    await _activeSessionsHub.Clients.All.SendAsync(terminalEndpoint, batch, cancellationToken);
                    batchIndex = 0;
                    continue;
                }
            }

            if (batchIndex > 0)
            {
                var remainingBatch = batch.Take(batchIndex);
                await _activeSessionsHub.Clients.All.SendAsync(terminalEndpoint, remainingBatch, cancellationToken);
            }

            return monkey;
        }

        public async Task UpdateSessionStatus(Session session, SessionStatus status, MonkifyTyper? monkey = null)
        {
            try
            {
                SessionResult? result = null;

                if (Session.SessionEndedStatus.Contains(status))
                {
                    session.EndDate = DateTime.UtcNow;
                    _context.Entry(session).Property(x => x.EndDate).IsModified = true;

                    if (monkey != null)
                    {
                        session.Seed = monkey.SessionSeed;
                        session.WinningChoice = monkey.FirstChoiceTyped;
                        _context.Entry(session).Property(x => x.Seed).IsModified = true;
                        _context.Entry(session).Property(x => x.WinningChoice).IsModified = true;

                        var winners = session.Bets.Where(x => x.Choice == monkey.FirstChoiceTyped).Select(x => x.Wallet).Distinct();
                        result = new SessionResult(winners, monkey.NumberOfWinners, monkey.FirstChoiceTyped);
                    }
                }

                await _context.SessionStatusLogs.AddAsync(new SessionStatusLog(session.Id, session.Status, status));

                session.Status = status;
                _context.Entry(session).Property(x => x.Status).IsModified = true;
                
                if (status == SessionStatus.SessionStarting)
                {
                    session.StartDate = DateTime.UtcNow.AddSeconds(_settings.Sessions.TimeUntilSessionStarts);
                    _context.Entry(session).Property(x => x.StartDate).IsModified = true;
                }

                await _context.SaveChangesAsync();

                var statusJson = new SessionStatusUpdatedEvent(status, session.StartDate, result).AsJson();
                string sessionStatusEndpoint = string.Format(_settings.Sessions.SessionStatusEndpoint, session.Id.ToString());
                await _activeSessionsHub.Clients.All.SendAsync(sessionStatusEndpoint, statusJson);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to change status for session {0}. CurrentStatus: {1}, New status {2}.", session.Id, session.Status, status);
            }
        }

        public async Task<bool> TryStartSession(Session session)
        {
            try
            {
                session.StartDate = DateTime.UtcNow.AddSeconds(_settings.Sessions.TimeUntilSessionStarts);

                var affectedRows = await _context.Database.ExecuteSqlRawAsync(
                    @"UPDATE Sessions 
                      SET Status = {0}, 
                          StartDate = {1},
                          UpdatedDate = {2}
                      WHERE Id = {3} AND Status = {4}",
                    (int)SessionStatus.SessionStarting,
                    session.StartDate,
                    DateTime.UtcNow,
                    session.Id,
                    (int)SessionStatus.WaitingBets
                );

                if (affectedRows > 0)
                {
                    await _context.SessionStatusLogs.AddAsync(new SessionStatusLog(session.Id, SessionStatus.WaitingBets, SessionStatus.SessionStarting));
                    await _context.SaveChangesAsync();

                    var statusJson = new SessionStatusUpdatedEvent(SessionStatus.SessionStarting, session.StartDate, null).AsJson();
                    string sessionStatusEndpoint = string.Format(_settings.Sessions.SessionStatusEndpoint, session.Id.ToString());
                    await _activeSessionsHub.Clients.All.SendAsync(sessionStatusEndpoint, statusJson);

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to start session {0}", session.Id);
            }

            return false;
        }

        public async Task UpdateBetStatus(IEnumerable<Bet> bets, BetStatus status)
        {
            foreach (var bet in bets)
                await UpdateBetStatusWithoutSaving(bet, status);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to change status multiple bets. Attemp on bets: {0}. New Status: {1}", bets.AsJson(), status);
            }
        }

        public async Task UpdateBetStatus(Bet bet, BetStatus status)
        {
            await UpdateBetStatusWithoutSaving(bet, status);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Failed to change status for bet {0}. CurrentStatus: {1}, New status {2}.", bet.Id, bet.Status, status);
            }
        }

        public void CloseOpenSessions()
        {
            try
            {
                var activeSessions = _context.Sessions
                .Include(x => x.Bets)
                .Where(x => Session.SessionInProgressStatus.Contains(x.Status))
                .AsNoTracking().ToList();

                if (activeSessions.IsNullOrEmpty())
                    return;

                foreach (var session in activeSessions)
                {
                    UpdateSessionStatus(session, SessionStatus.SessionEndedAbruptely).Wait();

                    foreach (var bet in session.Bets)
                    {
                        UpdateBetStatus(bet, BetStatus.NeedsManualAnalysis).Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to close the previous open sessions.");
            }
        }

        public async Task CreateSession(SessionParameters parameters)
        {
            try
            {
                var session = new Session(parameters.Id);

                await _context.Sessions.AddAsync(session);
                await _context.SaveChangesAsync();

                _sessionTracker.AddSession(session.Id);

                session.Parameters = parameters;

                var sessionCreatedEvent = new SessionCreatedEvent(session.Id, parameters);
                await _activeSessionsHub.Clients.All.SendAsync(_settings.Sessions.ActiveSessionsEndpoint, sessionCreatedEvent.AsJson());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create session for parameter {0}.", parameters.Id);
            }
        }

        private async Task UpdateBetStatusWithoutSaving(Bet bet, BetStatus status)
        {
            await _context.BetStatusLogs.AddAsync(new BetStatusLog(bet.Id, bet.Status, status));
            bet.Status = status;
            _context.Entry(bet).Property(x => x.Status).IsModified = true;
        }
    }
}
