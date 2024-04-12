﻿using MediatR;
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
    public class RefundSession : BaseWorker
    {
        public RefundSession(IServiceProvider services) : base(services) { }

        protected override async Task ExecuteProcess(CancellationToken cancellationToken)
        {
            using (var scope = Services.CreateScope())
            {
                var settings = scope.GetService<GeneralSettings>();
                var context = scope.GetService<MonkifyDbContext>();
                var solanaService = scope.GetService<ISolanaService>();
                var sessionService = scope.GetService<ISessionService>();

                var sessionsToBeRefunded = await context.Sessions
                    .Where(x => x.Status == SessionStatus.NeedsRefund)
                    .ToListAsync(cancellationToken);

                if (!sessionsToBeRefunded.Any())
                    return;

                var betValidator = new BetValidator(settings.Token);

                foreach(var session in sessionsToBeRefunded)
                {
                    var sessionBets = await context.SessionBets
                        .Include(x => x.Logs)
                        .Include(x => x.User)
                        .Where(x => x.SessionId == session.Id && !x.Refunded && !x.Won)
                        .ToListAsync();

                    await sessionService.UpdateSessionStatus(session, SessionStatus.RefundingPlayers);

                    if (sessionBets.IsNullOrEmpty())
                    {
                        await sessionService.UpdateSessionStatus(session, SessionStatus.PlayersRefunded);
                        continue;
                    }

                    bool successInAllRefunds = true;

                    foreach(var bet in sessionBets)
                    {
                        if (bet.Refunded)
                            continue;

                        var refundResult = betValidator.CalculateRefundForBet(bet);
                        successInAllRefunds &= await solanaService.TransferRefundTokens(bet, refundResult);
                    }

                    if (successInAllRefunds)
                        await sessionService.UpdateSessionStatus(session, SessionStatus.PlayersRefunded);
                    else
                        await sessionService.UpdateSessionStatus(session, SessionStatus.NeedsRefund);
                }
            }
        }
    }
}
