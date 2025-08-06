using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Abstractions.KafkaHandlers;
using Monkify.Infrastructure.Consumers.GameSessionProcessor;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Sessions;

namespace Monkify.Infrastructure.Consumers.BetPlaced
{
    public class BetPlacedConsumer : BaseConsumer<BetPlacedEvent>
    {
        public BetPlacedConsumer(IServiceProvider services, IOptionsMonitor<ConsumerOptions> options) : base(options) { _services = services; }

        private IServiceProvider _services;

        protected override async Task ConsumeAsync(BetPlacedEvent message, CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonkifyDbContext>();
            var sessionSettings = scope.ServiceProvider.GetRequiredService<GeneralSettings>();
            var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

            var session = await context.Sessions
                .Include(x => x.Parameters)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == message.SessionId && x.Status == SessionStatus.WaitingBets, cancellationToken);

            if (session == null)
                return;

            var betsPlaced = await context.SessionBets
                .Where(b => b.SessionId == message.SessionId)
                .GroupBy(x => new { x.Wallet, x.Choice })
                .CountAsync(cancellationToken);

            var elapsedTimeSinceCreation = (DateTime.UtcNow - session.CreatedDate).TotalSeconds;

            bool minimumTimeElapsed = elapsedTimeSinceCreation > sessionSettings.Sessions.MinimumWaitPeriodForBets;
            bool sessionHasEnoughPlayers = betsPlaced >= session.Parameters.MinimumNumberOfPlayers;

            if (!minimumTimeElapsed || !sessionHasEnoughPlayers)
                return;

            await sessionService.UpdateSessionStatus(session, SessionStatus.SessionStarting);

            var producer = scope.ServiceProvider.GetRequiredService<KafkaProducer<GameSessionProcessorEvent>>();
            await producer.ProduceAsync(new GameSessionProcessorEvent(session));
        }
    }
}
