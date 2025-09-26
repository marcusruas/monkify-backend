using MediatR;
using Microsoft.EntityFrameworkCore;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Background.Events.RewardWinners;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Sessions;
using Serilog;
using static Monkify.Domain.Sessions.ValueObjects.SessionStatus;

namespace Monkify.Infrastructure.Background.Events.StartSession
{
    public class StartSessionHandler : BaseNotificationHandler<StartSessionEvent>
    {
        public StartSessionHandler(MonkifyDbContext context, IMediator mediator, ISessionService sessionService, GeneralSettings settings, SessionBetsTracker tracker)
        {
            _context = context;
            _mediator = mediator;
            _sessionService = sessionService;
            _sessionSettings = settings.Sessions;            _tracker = tracker;

        }

        private readonly MonkifyDbContext _context;
        private readonly IMediator _mediator;
        private readonly ISessionService _sessionService;
        private readonly SessionSettings _sessionSettings;
        private readonly SessionBetsTracker _tracker;

        private Session _session;
        private MonkifyTyper _monkey;

        public override async Task HandleRequest(StartSessionEvent notification, CancellationToken cancellationToken)
         {
            _session = notification.Session;

            await PrepareSessionForStart(cancellationToken);

            _monkey = await _sessionService.RunSession(_session, cancellationToken);

            await _sessionService.UpdateSessionStatus(_session, Ended, _monkey);

            await _tracker.RemoveSessionAsync(_session.Id);

           await DeclareWinners();

            await Task.Delay(_sessionSettings.DelayBetweenSessions * 1000, cancellationToken);

            await _sessionService.CreateSession(notification.Session.Parameters);
        }

        private async Task PrepareSessionForStart(CancellationToken cancellationToken)
        {
            var delay = Convert.ToInt32((_session.StartDate - DateTime.UtcNow).Value.TotalMilliseconds);

            if (delay <= 0) delay = 1;

            await Task.Delay(delay, cancellationToken);

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
