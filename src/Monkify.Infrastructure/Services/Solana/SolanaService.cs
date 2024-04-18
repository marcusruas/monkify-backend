using Microsoft.EntityFrameworkCore;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Sessions;
using Serilog;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Core.Http;
using Solnet.Wallet;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Services.Solana
{
    public class SolanaService : ISolanaService
    {
        public SolanaService(MonkifyDbContext context, ISessionService sessionService, IRpcClient rpcClient, GeneralSettings settings)
        {
            _context = context;
            _sessionService = sessionService;
            _rpcClient = rpcClient;
            _settings = settings;

            _ownerAccount ??= new Account(Convert.FromBase64String(_settings.Token.TokenOwnerPrivateKey), new PublicKey(_settings.Token.TokenOwnerPublicKey).KeyBytes);
        }

        private readonly MonkifyDbContext _context;
        private readonly ISessionService _sessionService;
        private readonly IRpcClient _rpcClient;
        private readonly GeneralSettings _settings;

        private Account _ownerAccount;
        private string _latestBlockhashAddress;

        public async Task<bool> SetLatestBlockhashForTokenTransfer()
        {
            var latestBlockHash = await _rpcClient.GetLatestBlockHashAsync();

            if (!latestBlockHash.WasSuccessful || string.IsNullOrWhiteSpace(latestBlockHash.Result?.Value?.Blockhash))
            {
                Log.Error("Failed to get latest blockhash from solana client. Reason: {0}, Details: {1}", latestBlockHash.Reason, latestBlockHash.RawRpcResponse);
                return false;
            }

            _latestBlockhashAddress = latestBlockHash.Result.Value.Blockhash;
            return true;
        }

        public async Task<bool> TransferTokensForBet(Bet bet, BetTransactionAmountResult amount)
        {
            if (amount.ValueInTokens == 0)
            {
                Log.Warning("An attempt to make a transaction with 0 tokens has been made. Bet: {0}, ErrorMessage: {1}", bet.Id, amount.ErrorMessage);
                return true;
            }

            try
            {
                var transferInstruction = TokenProgram.Transfer(new PublicKey(_settings.Token.SenderAccount), new PublicKey(bet.Wallet), amount.ValueInTokens, _ownerAccount.PublicKey);

                var transaction = new TransactionBuilder()
                        .SetRecentBlockHash(_latestBlockhashAddress)
                        .SetFeePayer(_ownerAccount)
                        .AddInstruction(transferInstruction)
                        .Build(new List<Account> { _ownerAccount });

                RequestResult<string> result = await _rpcClient.SendTransactionAsync(transaction);

                if (!result.WasSuccessful)
                {
                    Log.Error("Failed to transfer funds to the bet's wallet. Value: {1}. Details: {2} ", bet.Id, amount.AsJson(), result.RawRpcResponse);
                    return false;
                }

                await _context.TransactionLogs.AddAsync(new TransactionLog(bet.Id, amount.Value, result.Result));
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to reward the bet {0}. Value: {1}", bet.Id, amount.AsJson().ToString());
                return false;
            }
        }
    }
}
