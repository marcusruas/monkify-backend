using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Monkify.Common.Extensions;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Context;
using Serilog;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Core.Http;
using Solnet.Wallet;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Background.Workers
{
    public class RefundSession : BaseWorker
    {
        public RefundSession(IServiceProvider services) : base(services) { }

        protected override async Task ExecuteProcess(CancellationToken cancellationToken)
        {
            using (var scope = Services.CreateScope())
            {
                var settings = scope.GetService<GeneralSettings>();
                var context = scope.GetService<MonkifyDbContext>();
                var solanaClient = scope.GetService<IRpcClient>();

                var sessionsToBeRefunded = await context.Sessions.Include(x => x.Bets).ThenInclude(x => x.Logs).Where(x => x.Status == SessionStatus.NeedsRefund).ToListAsync(cancellationToken);

                if (!sessionsToBeRefunded.Any())
                    return;

                var betValidator = new BetValidator(settings.Token);

                foreach(var session in sessionsToBeRefunded)
                {
                    await UpdateSessionStatus(context, session, SessionStatus.RefundingPlayers);

                    if (session.Bets.IsNullOrEmpty())
                    {
                        await UpdateSessionStatus(context, session, SessionStatus.PlayersRefunded);
                        continue;
                    }

                    foreach(var bet in session.Bets)
                    {
                        var refundResult = betValidator.CalculateRefundForBet(bet);
                    }                    

                    await UpdateSessionStatus(context, session, SessionStatus.PlayersRefunded);
                }
            }
        }

        private async Task UpdateSessionStatus(MonkifyDbContext context, Session session, SessionStatus status)
        {
            await context.SessionLogs.AddAsync(new SessionLog(session.Id, session.Status, status));

            session.UpdateStatus(status);
            context.Sessions.Update(session);
            await context.SaveChangesAsync();
        }
    }
}
