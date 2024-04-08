using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Context;
using Newtonsoft.Json;
using Serilog;
using System.Collections.ObjectModel;
using static Monkify.Domain.Sessions.ValueObjects.SessionStatus;

namespace Monkify.Infrastructure.Handlers.Sessions.Events
{
    public class ProcessSession : BaseNotificationHandler<SessionCreated>
    {
        public ProcessSession(MonkifyDbContext context, GeneralSettings settings, IHubContext<ActiveSessionsHub> hub)
        {
            _context = context;
            _activeSessions = hub;
            _sessionSettings = settings.Sessions;
        }

        private readonly MonkifyDbContext _context;
        private readonly IHubContext<ActiveSessionsHub> _activeSessions;
        private readonly SessionSettings _sessionSettings;

        private Session _session;
        private bool _sessionHasEnoughPlayers;

        private MonkifyTyper _monkey;

        public override async Task HandleRequest(SessionCreated notification, CancellationToken cancellationToken)
        {
            await Task.Delay(_sessionSettings.WaitPeriodForBets * 1000);

            _session = await _context.Sessions.Include(x => x.Bets).AsNoTracking().FirstOrDefaultAsync(x => x.Id == notification.SessionId);
            _sessionHasEnoughPlayers = _session.Bets.DistinctBy(x => x.UserId)?.Count() >= notification.MinimumNumberOfPlayers;

            if (!_sessionHasEnoughPlayers)
            {
                _session.Bets.Clear();

                await UpdateSessionStatus(NotEnoughPlayersToStart);
                await UpdateSessionStatus(NeedsRefund);

                await Task.Delay(_sessionSettings.DelayBetweenSessions * 1000, cancellationToken);
                return;
            }

            _monkey = new MonkifyTyper(notification.CharacterType, _session.Bets);
            _session.Bets.Clear();

            await UpdateSessionStatus(Started);
            await SendTerminalCharacters(notification);
            await UpdateSessionStatus(Ended);
            await DeclareWinners();

            await Task.Delay(_sessionSettings.DelayBetweenSessions * 1000, cancellationToken);
        }

        private async Task UpdateSessionStatus(SessionStatus status)
        {
            await _context.SessionLogs.AddAsync(new SessionLog(_session.Id, _session.Status, status));
            
            _session.UpdateStatus(status);
            _context.Sessions.Update(_session);
            await _context.SaveChangesAsync();

            SessionResult? result = null;

            if (status == Ended)
                result = new SessionResult(_monkey.NumberOfWinners, _monkey.FirstChoiceTyped);

            var sessionJson = JsonConvert.SerializeObject(new SessionStatusUpdated(status, result));
            string sessionStatusEndpoint = string.Format(_sessionSettings.SessionStatusEndpoint, _session.Id.ToString());
            await _activeSessions.Clients.All.SendAsync(sessionStatusEndpoint, sessionJson);
        }

        private async Task SendTerminalCharacters(SessionCreated notification)
        {
            
            string terminalEndpoint = string.Format(_sessionSettings.SessionTerminalEndpoint, notification.SessionId.ToString());
            List<char> batch = new(_sessionSettings.TerminalBatchLimit);

            while (!_monkey.HasWinners)
            {
                var character = _monkey.GenerateNextCharacter();
                batch.Add(character);
                if (batch.Count >= _sessionSettings.TerminalBatchLimit)
                {
                    await Task.Delay(_sessionSettings.DelayBetweenTerminalBatches);
                    await _activeSessions.Clients.All.SendAsync(terminalEndpoint, batch);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await Task.Delay(_sessionSettings.DelayBetweenTerminalBatches);
                await _activeSessions.Clients.All.SendAsync(terminalEndpoint, batch);
                batch.Clear();
            }
        }

        private async Task DeclareWinners()
        {
            if (!_monkey.HasWinners)
                return;
            
            _context.SessionBets.UpdateRange(_monkey.Bets);
            await _context.SaveChangesAsync();
        }
    }
}
