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
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime;
using System.Threading;
using System.Threading.Channels;
using static Monkify.Domain.Sessions.ValueObjects.SessionStatus;

namespace Monkify.Infrastructure.Background.Events
{
    public class SessionStart : BaseNotificationHandler<SessionStartEvent>
    {
        public SessionStart(MonkifyDbContext context, IMediator mediator, ISessionService sessionService, GeneralSettings settings)
        {
            _context = context;
            _mediator = mediator;
            _sessionService = sessionService;
            _sessionSettings = settings.Sessions;
        }

        private readonly MonkifyDbContext _context;
        private readonly IMediator _mediator;
        private readonly ISessionService _sessionService;
        private readonly SessionSettings _sessionSettings;

        private Session _session;
        private MonkifyTyper _monkey;

        public override async Task HandleRequest(SessionStartEvent notification, CancellationToken cancellationToken)
        {
            _session = notification.Session;

            await WaitForMinimumAmountOfPlayers(cancellationToken);
            await PrepareSessionForStart(cancellationToken);

            _monkey = await _sessionService.RunSession(_session, cancellationToken);

            await _sessionService.UpdateSessionStatus(_session, Ended, _monkey);
            await DeclareWinners();

            await Task.Delay(_sessionSettings.DelayBetweenSessions * 1000, cancellationToken);
        }

        private async Task WaitForMinimumAmountOfPlayers(CancellationToken cancellationToken)
        {
            bool sessionHasEnoughPlayers = false;
            bool minimumTimeElapsed = false;

            while (!minimumTimeElapsed || !sessionHasEnoughPlayers)
            {
                var elapsedTimeSinceCreation = (DateTime.UtcNow - _session.CreatedDate).TotalSeconds;
                minimumTimeElapsed = elapsedTimeSinceCreation > _sessionSettings.MinimumWaitPeriodForBets;

                if (!sessionHasEnoughPlayers)
                {
                    var playerCount = await _context.SessionBets.Where(x => x.SessionId == _session.Id).GroupBy(x => new { x.Wallet, x.Choice }).AsNoTracking().CountAsync();
                    sessionHasEnoughPlayers = playerCount >= _session.Parameters.MinimumNumberOfPlayers;
                }

                await Task.Delay(2000, cancellationToken);
            }
        }

        private async Task PrepareSessionForStart(CancellationToken cancellationToken)
        {
            await _sessionService.UpdateSessionStatus(_session, SessionStarting);

            while (DateTime.UtcNow <= _session.StartDate)
            {
                await Task.Delay(1000, cancellationToken);
            }

            await _sessionService.UpdateSessionStatus(_session, InProgress);

            _session.Bets = await _context.SessionBets.Where(x => x.SessionId == _session.Id).ToListAsync(cancellationToken);
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
