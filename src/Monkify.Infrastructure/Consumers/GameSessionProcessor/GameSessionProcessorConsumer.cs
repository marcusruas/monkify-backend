using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Abstractions.KafkaHandlers;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Sessions;

namespace Monkify.Infrastructure.Consumers.GameSessionProcessor
{
    public class GameSessionProcessorConsumer : BaseConsumer<GameSessionProcessorEvent>
    {
        public GameSessionProcessorConsumer(IServiceProvider services, IOptionsMonitor<ConsumerOptions> options) : base(options) { _services = services; }

        private readonly IServiceProvider _services;

        protected override async Task ConsumeAsync(GameSessionProcessorEvent message, CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonkifyDbContext>();
            var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

            while (DateTime.UtcNow <= message.Session.StartDate)
            {
                await Task.Delay(1000, cancellationToken);
            }

            await sessionService.UpdateSessionStatus(message.Session, SessionStatus.InProgress);

            message.Session.Bets = await context.SessionBets.Where(x => x.SessionId == message.Session.Id).ToListAsync(cancellationToken);
            var gameResult = await sessionService.RunSession(message.Session, cancellationToken);

            await sessionService.UpdateSessionStatus(message.Session, SessionStatus.Ended, gameResult);

            if (!gameResult.HasWinners)
                return;

            await sessionService.UpdateBetStatus(message.Session.Bets.Where(x => x.Choice == gameResult.FirstChoiceTyped), BetStatus.NeedsRewarding);

            var producer = scope.ServiceProvider.GetRequiredService<KafkaProducer<RewardWinnersEvent>>();
            await producer.ProduceAsync(new RewardWinnersEvent(message.Session));
        }

    }
}
