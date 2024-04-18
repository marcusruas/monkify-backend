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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Services.Sessions
{
    public class SessionService : ISessionService
    {
        public SessionService(GeneralSettings settings, MonkifyDbContext context, IHubContext<ActiveSessionsHub> activeSessionsHub)
        {
            _settings = settings;
            _context = context;
            _activeSessionsHub = activeSessionsHub;
        }

        private readonly GeneralSettings _settings;
        private readonly MonkifyDbContext _context;
        private readonly IHubContext<ActiveSessionsHub> _activeSessionsHub;

        public async Task UpdateSessionStatus(Session session, SessionStatus status, MonkifyTyper? monkey = null)
        {
            try
            {
                SessionResult? result = null;

                if (status == SessionStatus.Ended && monkey != null)
                {
                    result = new SessionResult(monkey.NumberOfWinners, monkey.FirstChoiceTyped);
                    _context.Entry(session).Property(x => x.WinningChoice).IsModified = true;
                }

                await _context.SessionStatusLogs.AddAsync(new SessionStatusLog(session.Id, session.Status, status));

                session.UpdateStatus(status, monkey?.FirstChoiceTyped);
                _context.Entry(session).Property(x => x.Status).IsModified = true;
                _context.Entry(session).Property(x => x.EndDate).IsModified = true;
                
                await _context.SaveChangesAsync();

                var statusJson = new SessionStatusUpdated(status, result).AsJson();
                string sessionStatusEndpoint = string.Format(_settings.Sessions.SessionStatusEndpoint, session.Id.ToString());
                await _activeSessionsHub.Clients.All.SendAsync(sessionStatusEndpoint, statusJson);
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Failed to change status for session {0}. CurrentStatus: {1}, New status {2}.", session.Id, session.Status, status);
            }
        }

        public async Task UpdateBetStatus(IEnumerable<Bet> bets, BetStatus status)
        {
            foreach(var bet in bets)
                await UpdateBetStatusWithoutSaving(bet, status);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateBetStatus(Bet bet, BetStatus status)
        {
            await UpdateBetStatusWithoutSaving(bet, status);
            await _context.SaveChangesAsync();
        }

        private async Task UpdateBetStatusWithoutSaving(Bet bet, BetStatus status)
        {
            await _context.BetStatusLogs.AddAsync(new BetStatusLog(bet.Id, bet.Status, status));
            bet.Status = status;
            _context.Entry(bet).Property(x => x.Status).IsModified = true;
        }
    }
}
