using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Monkify.Common.Extensions;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Sessions;
using Monkify.Infrastructure.Services.Solana;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Background.Workers
{
    public class RewardSessions : BaseWorker
    {
        public RewardSessions(IServiceProvider services) : base(services) { }

        public override async Task ExecuteProcess(CancellationToken cancellationToken)
        {
            using var scope = Services.CreateScope();

            var settings = scope.GetService<GeneralSettings>();
            var context = scope.GetService<MonkifyDbContext>();
            var mediator = scope.GetService<IMediator>();

            var sessionsToBeRewarded = await context.Sessions
                .Include(x => x.Bets).ThenInclude(x => x.TransactionLogs)
                .Where(x => 
                    x.Status == SessionStatus.ErrorWhenProcessingRewards 
                 && x.CreatedDate > DateTime.UtcNow.AddMinutes(-60) 
                 && x.Bets.Any(x => x.Status == BetStatus.NeedsRewarding)
                 )
                .ToListAsync(cancellationToken);

            if (!sessionsToBeRewarded.Any())
            {
                await Task.Delay(settings.Workers.RewardSessionsInterval * 1000, cancellationToken);
                return;
            }

            foreach (var session in sessionsToBeRewarded)
                await mediator.Publish(new RewardWinnersEvent(session), cancellationToken);
        }
    }
}
