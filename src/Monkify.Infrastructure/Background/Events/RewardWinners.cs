using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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
using Newtonsoft.Json;
using Serilog;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Core.Http;
using Solnet.Wallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Background.Events
{
    public class RewardWinners : BaseNotificationHandler<RewardWinnersEvent>
    {
        public RewardWinners(ISolanaService client, ISessionService sessionService, GeneralSettings settings)
        {
            _solanaService = client;
            _sessionService = sessionService;
            _settings = settings;
        }

        private readonly ISolanaService _solanaService;
        private readonly ISessionService _sessionService;
        private readonly GeneralSettings _settings;

        private BetDomainService _betService;

        public override async Task HandleRequest(RewardWinnersEvent notification, CancellationToken cancellationToken)
        {
            _betService = new(notification.Session, _settings.Token);

            bool solanaIsUp = await _solanaService.SetLatestBlockhashForTokenTransfer();

            if (!solanaIsUp)
                return;

            bool allBetsRewarded = true;

            await _sessionService.UpdateSessionStatus(notification.Session, SessionStatus.RewardForWinnersInProgress);

            foreach (var winner in _betService.Winners)
            {
                var rewardResult = _betService.CalculateRewardForBet(winner);

                if (string.IsNullOrWhiteSpace(rewardResult.ErrorMessage))
                {
                    bool transactionSuccessful = await _solanaService.TransferTokensForBet(winner, rewardResult);

                    if (transactionSuccessful)
                        await _sessionService.UpdateBetPaymentStatus(winner, BetPaymentStatus.Rewarded);

                    allBetsRewarded &= transactionSuccessful;

                    continue;
                }

                Log.Warning("Bet {0} could not be rewarded due to an error. details: {1}", winner.Id, rewardResult.ErrorMessage);

                if (rewardResult.ErrorMessage == ErrorMessages.BetHasAlreadyBeenRewarded)
                    await _sessionService.UpdateBetPaymentStatus(winner, BetPaymentStatus.Rewarded);

                if (rewardResult.ErrorMessage == ErrorMessages.BetRewardBiggerThanThePot)
                    await _sessionService.UpdateBetPaymentStatus(winner, BetPaymentStatus.NeedsManualAnalysis);
            }

            if (allBetsRewarded)
                await _sessionService.UpdateSessionStatus(notification.Session, SessionStatus.RewardForWinnersCompleted);
            else
                await _sessionService.UpdateSessionStatus(notification.Session, SessionStatus.NeedsRewarding);
        }
    }
}
