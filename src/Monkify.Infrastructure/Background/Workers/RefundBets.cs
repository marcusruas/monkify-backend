using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Monkify.Common.Extensions;
using Monkify.Common.Resources;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Sessions;
using Monkify.Infrastructure.Services.Solana;
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
    public class RefundBets : BaseWorker
    {
        public RefundBets(IServiceProvider services) : base(services) { }

        protected override async Task ExecuteProcess(CancellationToken cancellationToken)
        {
            using var scope = Services.CreateScope();

            var settings = scope.GetService<GeneralSettings>();
            var context = scope.GetService<MonkifyDbContext>();
            var solanaService = scope.GetService<ISolanaService>();
            var sessionService = scope.GetService<ISessionService>();

            var betsToBeRefunded = await context.SessionBets
                .Include(x => x.TransactionLogs)
                .Where(x => x.Status == BetStatus.NeedsRefunding)
                .ToListAsync();

            if (!betsToBeRefunded.Any())
            {
                await Task.Delay(settings.Workers.RefundBetsInterval * 1000, cancellationToken);
                return;
            }

            foreach (var bet in betsToBeRefunded)
            {
                var refundResult = BetDomainService.CalculateRefundForBet(settings.Token, bet);

                if (refundResult.Value > 0)
                {
                    bool betRefunded = await solanaService.TransferTokensForBet(bet, refundResult);

                    if (betRefunded)
                        await sessionService.UpdateBetStatus(bet, BetStatus.Refunded);

                    continue;
                }

                if (refundResult.ErrorMessage == ErrorMessages.BetHasAlreadyBeenRefunded)
                    await sessionService.UpdateBetStatus(bet, BetStatus.Refunded);        
                else
                    Log.Warning("Bet {0} has already been properly refunded. Value needing to be refunded: {1}", bet.Id, refundResult.Value);
            }

            await Task.Delay(settings.Workers.RefundBetsInterval * 1000, cancellationToken);
            return;
        }
    }
}
