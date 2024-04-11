using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Context;
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
    public class RewardWinnersHandler : BaseNotificationHandler<RewardWinnersEvent>
    {
        public RewardWinnersHandler(MonkifyDbContext context, IRpcClient client, IHubContext<ActiveSessionsHub> hub, GeneralSettings settings)
        {
            _context = context;
            _solanaClient = client;
            _hub = hub;
            _settings = settings;
        }

        private readonly MonkifyDbContext _context;
        private readonly IRpcClient _solanaClient;
        private readonly IHubContext<ActiveSessionsHub> _hub;
        private readonly GeneralSettings _settings;

        private BetValidator _betValidator;
        private Guid _sessionId;
        private string _blockhashAddress;
        private Dictionary<Guid, string> _winnerWallets;

        public override async Task HandleRequest(RewardWinnersEvent notification, CancellationToken cancellationToken)
        {
            await GetWinnerWallets(notification);

            await UpdateSessionStatus(notification.Session, SessionStatus.RewardForWinnersInProgress);

            await GetLatestBlockHash(notification);
            await RewardWinners();

            await UpdateSessionStatus(notification.Session, SessionStatus.RewardForWinnersCompleted);
        }

        private async Task GetWinnerWallets(RewardWinnersEvent notification)
        {
            _betValidator = new(notification.Session, _settings.Token);

            var winnerIds = _betValidator.Winners.Select(x => x.UserId);
            _winnerWallets = await _context.Users.Where(x => winnerIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Wallet);
        }

        private async Task GetLatestBlockHash(RewardWinnersEvent notification)
        {
            var latestBlockHash = await _solanaClient.GetLatestBlockHashAsync();

            if (!latestBlockHash.WasSuccessful || string.IsNullOrWhiteSpace(latestBlockHash.Result?.Value?.Blockhash))
            {
                Log.Error("Failed to get latest blockhash from solana client. Reason: {0}, Details: {1}", latestBlockHash.Reason, latestBlockHash.RawRpcResponse);
                await UpdateSessionStatus(notification.Session, SessionStatus.NeedsRefund);
                throw new Exception("Error in solana client. Check logs.");
            }

            _blockhashAddress = latestBlockHash.Result.Value.Blockhash;
        }

        private async Task RewardWinners()
        {
            foreach (var winner in _betValidator.Winners)
            {
                var rewardResult = _betValidator.CalculateRewardForBet(winner);

                try
                {
                    var ownerAccount = new Account(Convert.FromBase64String(_settings.Token.TokenOwnerPrivateKey), new PublicKey(_settings.Token.TokenOwnerPublicKey).KeyBytes);
                    var transferInstruction = TokenProgram.Transfer(new PublicKey(_settings.Token.SenderAccount), new PublicKey(_winnerWallets[winner.UserId]), rewardResult.RewardInTokens, ownerAccount.PublicKey);

                    var transaction = new TransactionBuilder()
                            .SetRecentBlockHash(_blockhashAddress)
                            .SetFeePayer(ownerAccount)
                            .AddInstruction(transferInstruction)
                            .Build(new List<Account> { ownerAccount });

                    RequestResult<string> result = await _solanaClient.SendTransactionAsync(transaction);

                    if (!result.WasSuccessful)
                        Log.Error("Failed to transfer funds to the bet's wallet. Value: {1}. Details: {2} ", winner.Id, rewardResult.AsJson(), result.RawRpcResponse);

                    await _context.BetLogs.AddAsync(new BetTransactionLog(winner, rewardResult.Reward, _winnerWallets[winner.UserId], result.Result));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to reward the bet {0}. Value: {1}", winner.Id, rewardResult.AsJson().ToString());
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task UpdateSessionStatus(Session session, SessionStatus status)
        {
            await _context.SessionLogs.AddAsync(new SessionLog(session.Id, session.Status, status));

            session.UpdateStatus(status);
            _context.Sessions.Update(session);
            await _context.SaveChangesAsync();

            var sessionJson = new SessionStatusUpdated(status).AsJson();
            string sessionStatusEndpoint = string.Format(_settings.Sessions.SessionStatusEndpoint, session.Id.ToString());
            await _hub.Clients.All.SendAsync(sessionStatusEndpoint, sessionJson);
        }
    }
}
