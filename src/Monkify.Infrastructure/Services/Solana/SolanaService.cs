using Microsoft.EntityFrameworkCore;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Context;
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
        public SolanaService(MonkifyDbContext context, IRpcClient rpcClient, GeneralSettings settings)
        {
            _context = context;
            _rpcClient = rpcClient;
            _settings = settings;
        }

        private readonly MonkifyDbContext _context;
        private readonly IRpcClient _rpcClient;
        private readonly GeneralSettings _settings;

        private Account _ownerAccount;
        private string _latestBlockhashAddress;

        public async Task<bool> TransferRefundTokens(Bet bet, BetTransactionAmountResult amount)
        {
            var successfulTransaction = await TransferTokens(bet.Id, bet.Wallet, amount);

            if (!successfulTransaction)
                return false;

            try
            {
                bet.Refunded = true;
                _context.Entry(bet).Property(x => x.Refunded).IsModified = true;

                await _context.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Failed to change the status of the bet to refunded. The Transaction was still successful. Id: {0}", bet.Id);
            }

            return true;
        }

        public async Task<bool> TransferTokens(Guid betId, string walletId, BetTransactionAmountResult amount)
        {
            if (amount.ValueInTokens == 0)
                return true;

            await CheckForSolanaConnection();

            try
            {
                var transferInstruction = TokenProgram.Transfer(new PublicKey(_settings.Token.SenderAccount), new PublicKey(walletId), amount.ValueInTokens, _ownerAccount.PublicKey);

                var transaction = new TransactionBuilder()
                        .SetRecentBlockHash(_latestBlockhashAddress)
                        .SetFeePayer(_ownerAccount)
                        .AddInstruction(transferInstruction)
                        .Build(new List<Account> { _ownerAccount });

                RequestResult<string> result = await _rpcClient.SendTransactionAsync(transaction);
                
                if (!result.WasSuccessful)
                {
                    Log.Error("Failed to transfer funds to the bet's wallet. Value: {1}. Details: {2} ", betId, amount.AsJson(), result.RawRpcResponse);
                    return false;
                }

                await _context.BetLogs.AddAsync(new BetTransactionLog(amount.Value, result.Result, betId));
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to reward the bet {0}. Value: {1}", betId, amount.AsJson().ToString());
                return false;
            }
        }

        private async Task CheckForSolanaConnection()
        {
            if (_ownerAccount == null)
                _ownerAccount = new Account(Convert.FromBase64String(_settings.Token.TokenOwnerPrivateKey), new PublicKey(_settings.Token.TokenOwnerPublicKey).KeyBytes);

            if (!string.IsNullOrWhiteSpace(_latestBlockhashAddress))
                return;

            var latestBlockHash = await _rpcClient.GetLatestBlockHashAsync();

            if (!latestBlockHash.WasSuccessful || string.IsNullOrWhiteSpace(latestBlockHash.Result?.Value?.Blockhash))
            {
                Log.Error("Failed to get latest blockhash from solana client. Reason: {0}, Details: {1}", latestBlockHash.Reason, latestBlockHash.RawRpcResponse);
                throw new Exception("Error in solana client. Check logs.");
            }

            _latestBlockhashAddress = latestBlockHash.Result.Value.Blockhash;
        }
    }
}
