using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Background.Events
{
    public class RewardWinnersHandler : BaseNotificationHandler<RewardWinnersEvent>
    {
        public RewardWinnersHandler(MonkifyDbContext context, IRpcClient client, GeneralSettings settings)
        {
            _context = context;
            _solanaClient = client;
            _settings = settings.Token;
            _logs = new();
        }

        private readonly MonkifyDbContext _context;
        private readonly IRpcClient _solanaClient;
        private readonly TokenSettings _settings;

        private string _blockhashAddress;
        private Account _ownerAccount;
        private decimal _totalPotAmount;
        private IEnumerable<Bet> _winners;
        private Dictionary<Guid, string> _winnerWallets;
        private List<BetTransactionLog> _logs;

        public override async Task HandleRequest(RewardWinnersEvent notification, CancellationToken cancellationToken)
        {
            await GetWinnerWallets(notification);
            await GetLatestBlockHash();
            await RewardWinners();
            await SaveTransactionLogs();
        }

        private async Task GetWinnerWallets(RewardWinnersEvent notification)
        {
            _winners = notification.Session.Bets.Where(x => x.Won);
            var winnerIds = _winners.Select(x => x.UserId);
            _winnerWallets = await _context.Users.Where(x => winnerIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Wallet);
            _totalPotAmount = notification.Session.Bets.Sum(x => x.Amount);
            _totalPotAmount = _totalPotAmount * (1 - _settings.CommisionPercentage);
        }

        private async Task GetLatestBlockHash()
        {
            var latestBlockHash = await _solanaClient.GetLatestBlockHashAsync();

            _blockhashAddress = latestBlockHash.Result.Value.Blockhash;
            _ownerAccount = new Account(Convert.FromBase64String(_settings.TokenOwnerPrivateKey), new PublicKey(_settings.TokenOwnerPublicKey).KeyBytes);
        }

        private async Task RewardWinners()
        {
            foreach (var winner in _winners)
            {
                decimal reward = (_totalPotAmount / _winners.Count()) - winner.Amount;
                reward = Math.Round(reward, _settings.Decimals, MidpointRounding.ToZero);
                ulong rewardInTokens = (ulong)(reward * (decimal) Math.Pow(10, _settings.Decimals));

                try
                {
                    var transferInstruction = TokenProgram.Transfer(new PublicKey(_settings.SenderAccount), new PublicKey(_winnerWallets[winner.UserId]), rewardInTokens, _ownerAccount.PublicKey);

                    var transaction = new TransactionBuilder()
                            .SetRecentBlockHash(_blockhashAddress)
                            .SetFeePayer(_ownerAccount)
                            .AddInstruction(transferInstruction)
                            .Build(new List<Account> { _ownerAccount });

                    RequestResult<string> result = await _solanaClient.SendTransactionAsync(transaction);

                    if (!result.WasSuccessful)
                        Log.Error("Failed to transfer funds to the bet's wallet. Value: {1}. Details: {2} ", winner.Id, reward.ToString(), result.RawRpcResponse);

                    _logs.Add(new BetTransactionLog(winner, reward, _winnerWallets[winner.UserId], result.Result));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to reward the bet {0}. Value: {1}", winner.Id, reward.ToString());
                }
            }
        }

        private async Task SaveTransactionLogs()
        {
            await _context.BetLogs.AddRangeAsync(_logs);
            await _context.SaveChangesAsync();
        }
    }
}
