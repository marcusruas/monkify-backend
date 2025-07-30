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
using Monkify.Infrastructure.Hubs;
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

        public override async Task HandleRequest(RewardWinnersEvent notification, CancellationToken cancellationToken)
        {
            BetDomainService betService = new(notification.Session, _settings.Token);

            //Although this call is made inside the TransferTokensForBet method, we just wanna make sure the service can grab it before starting updating the session.
            var blockhash = await _solanaService.GetLatestBlockhashForTokenTransfer();

            if (string.IsNullOrEmpty(blockhash))
            {
                Log.Error("Failed to reward players of session {0} due to a Solana connection error");
                await _sessionService.UpdateSessionStatus(notification.Session, SessionStatus.ErrorWhenProcessingRewards);
                return;
            }

            await _sessionService.UpdateSessionStatus(notification.Session, SessionStatus.RewardForWinnersInProgress);

            foreach (var winner in betService.Winners)
            {
                var rewardResult = betService.CalculateRewardForBet(winner);

                if (string.IsNullOrWhiteSpace(rewardResult.ErrorMessage))
                {
                    bool currentBetRewarded = await _solanaService.TransferTokensForBet(winner, rewardResult);

                    if (currentBetRewarded)
                        await _sessionService.UpdateBetStatus(winner, BetStatus.Rewarded);

                    continue;
                }

                Log.Warning("Bet {0} could not be rewarded due to an error. details: {1}", winner.Id, rewardResult.ErrorMessage);

                if (rewardResult.ErrorMessage == ErrorMessages.BetHasAlreadyBeenRewarded)
                    await _sessionService.UpdateBetStatus(winner, BetStatus.Rewarded);

                if (rewardResult.ErrorMessage == ErrorMessages.BetRewardBiggerThanThePot)
                    await _sessionService.UpdateBetStatus(winner, BetStatus.NeedsManualAnalysis);
            }

            SessionStatus newSessionStatus = SessionStatus.RewardForWinnersCompleted;

            if (!betService.Winners.All(x => x.Status == BetStatus.Rewarded))
                newSessionStatus = SessionStatus.ErrorWhenProcessingRewards;

            await _sessionService.UpdateSessionStatus(notification.Session, newSessionStatus);
        }
    }
}
