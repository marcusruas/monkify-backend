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
using Monkify.Infrastructure.Abstractions.KafkaHandlers;
using Monkify.Infrastructure.Context;

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

            var session = await context.Sessions
                .Include(x => x.Bets)
                .Where(x => x.Id == message.SessionId)
                .Select(x => new { BetCount = x.Bets.GroupBy(x => new { x.Wallet, x.Choice }).Count(), x.CreatedDate })
                .FirstOrDefaultAsync();

            var elapsedTimeSinceCreation = (DateTime.UtcNow - session.CreatedDate).TotalSeconds;

            if (elapsedTimeSinceCreation < sessionSettings.Sessions.MinimumWaitPeriodForBets)
                return;
        }
    }
}
