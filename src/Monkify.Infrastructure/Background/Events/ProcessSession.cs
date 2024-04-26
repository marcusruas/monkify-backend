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
using System.Diagnostics;
using static Monkify.Domain.Sessions.ValueObjects.SessionStatus;

namespace Monkify.Infrastructure.Background.Events
{
    public class ProcessSession : BaseNotificationHandler<SessionForProcessing>
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
        private MonkifyTyper _monkey;

        public override async Task HandleRequest(SessionForProcessing notification, CancellationToken cancellationToken)
        {
            _session = notification.Session;

            var sessionHasEnoughPlayers = await WaitForBets(cancellationToken);

            if (!sessionHasEnoughPlayers)
            {
                await _sessionService.UpdateSessionStatus(_session, NotEnoughPlayersToStart);

                if (_session.Bets.Any())
                    await _sessionService.UpdateBetStatus(_session.Bets, BetStatus.NeedsRefunding);

                await Task.Delay(_sessionSettings.DelayBetweenSessions * 1000, cancellationToken);
                return;
            }

            _monkey = new MonkifyTyper(_session);

            await _sessionService.UpdateSessionStatus(_session, InProgress);
            await SendTerminalCharacters();
            await _sessionService.UpdateSessionStatus(_session, Ended, _monkey);
            await DeclareWinners();

            await Task.Delay(_sessionSettings.DelayBetweenSessions * 1000, cancellationToken);
        }

        private async Task<bool> WaitForBets(CancellationToken cancellationToken)
        {
            bool sessionHasEnoughPlayers = false;
            bool minimumTimeElapsed = false;
            bool maximumTimeElapsed = false;

            while ((!minimumTimeElapsed || !sessionHasEnoughPlayers) && !maximumTimeElapsed)
            {
                var elapsedTimeSinceCreation = (DateTime.UtcNow - _session.CreatedDate).TotalSeconds;
                minimumTimeElapsed = elapsedTimeSinceCreation > _sessionSettings.MinimumWaitPeriodForBets;
                maximumTimeElapsed = elapsedTimeSinceCreation > _sessionSettings.MaximumWaitPeriodForBets;

                if (!sessionHasEnoughPlayers)
                {
                    _session.Bets = await _context.SessionBets.Where(x => x.SessionId == _session.Id).ToListAsync();
                    sessionHasEnoughPlayers = _session.Bets.DistinctBy(x => x.Wallet)?.Count() >= _session.Parameters.MinimumNumberOfPlayers;
                }

                await Task.Delay(2000, cancellationToken);
            }

            return sessionHasEnoughPlayers;
        }

        private async Task SendTerminalCharacters()
        {
            string terminalEndpoint = string.Format(_sessionSettings.SessionTerminalEndpoint, _session.Id.ToString());
            List<char> batch = new(_sessionSettings.TerminalBatchLimit);

            while (!_monkey.HasWinners)
            {
                batch.Add(_monkey.GenerateNextCharacter());
                if (batch.Count >= _sessionSettings.TerminalBatchLimit)
                    await SendBatch(terminalEndpoint, batch);
            }

            if (batch.Count > 0)
                await SendBatch(terminalEndpoint, batch);
        }

        private async Task SendBatch(string endpoint, List<char> batch)
        {
            await Task.Delay(_sessionSettings.DelayBetweenTerminalBatches);
            await _activeSessions.Clients.All.SendAsync(endpoint, batch);
            batch.Clear();
        }

        private async Task DeclareWinners()
        {
            if (!_monkey.HasWinners)
                return;

            await _sessionService.UpdateBetStatus(_session.Bets.Where(x => x.Choice == _monkey.FirstChoiceTyped), BetStatus.NeedsRewarding);
            await _mediator.Publish(new RewardWinnersEvent(_session));
        }
    }
}
