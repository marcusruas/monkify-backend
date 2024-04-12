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
using Monkify.Infrastructure.Services.Sessions;
using Newtonsoft.Json;
using Serilog;
using System.Collections.ObjectModel;
using static Monkify.Domain.Sessions.ValueObjects.SessionStatus;

namespace Monkify.Infrastructure.Handlers.Sessions.Events
{
    public class ProcessSession : BaseNotificationHandler<SessionCreated>
    {
        public ProcessSession(MonkifyDbContext context, IMediator mediator, IHubContext<ActiveSessionsHub> hub, ISessionService sessionService, GeneralSettings settings)
        {
            _context = context;
            _mediator = mediator;
            _activeSessions = hub;
            _sessionService = sessionService;
            _sessionSettings = settings.Sessions;
        }

        private readonly MonkifyDbContext _context;
        private readonly IMediator _mediator;
        private readonly IHubContext<ActiveSessionsHub> _activeSessions;
        private readonly ISessionService _sessionService;
        private readonly SessionSettings _sessionSettings;

        private Session _session;
        private bool _sessionHasEnoughPlayers;

        private MonkifyTyper _monkey;

        public override async Task HandleRequest(SessionCreated notification, CancellationToken cancellationToken)
        {
            await Task.Delay(_sessionSettings.WaitPeriodForBets * 1000);

            _session = await _context.Sessions.Include(x => x.Bets).FirstOrDefaultAsync(x => x.Id == notification.SessionId);
            _sessionHasEnoughPlayers = _session.Bets.DistinctBy(x => x.Wallet)?.Count() >= notification.MinimumNumberOfPlayers;

            if (!_sessionHasEnoughPlayers)
            {
                _session.Bets.Clear();

                await _sessionService.UpdateSessionStatus(_session, NotEnoughPlayersToStart);
                await _sessionService.UpdateSessionStatus(_session, NeedsRefund);

                await Task.Delay(_sessionSettings.DelayBetweenSessions * 1000, cancellationToken);
                return;
            }

            _monkey = new MonkifyTyper(notification.CharacterType, _session.Bets);
            _session.Bets.Clear();

            await _sessionService.UpdateSessionStatus(_session, Started);
            await SendTerminalCharacters(notification);
            await _sessionService.UpdateSessionStatus(_session, Ended, _monkey);
            await DeclareWinners();

            await Task.Delay(_sessionSettings.DelayBetweenSessions * 1000, cancellationToken);
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

            _session.Bets = _monkey.Bets;
            _context.SessionBets.UpdateRange(_session.Bets);
            await _context.SaveChangesAsync();
            await _mediator.Publish(new RewardWinnersEvent(_session));
        }
    }
}
