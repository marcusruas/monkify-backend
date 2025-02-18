using Bogus.Bson;
using Monkify.Common.Resources;
using Monkify.Common.Results;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Context;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Core.Http;
using Solnet.Rpc.Messages;
using Solnet.Rpc.Models;
using Solnet.Rpc.Types;
using Solnet.Wallet;

namespace Monkify.Infrastructure.Services.Solana
{
    public class SolanaService : ISolanaService
    {
        public SolanaService(MonkifyDbContext context, IRpcClient rpcClient, GeneralSettings settings)
        {
            _context = context;
            _rpcClient = rpcClient;
            _settings = settings;

            _ownerAccount ??= new Account(_settings.Token.TokenOwnerPrivateKey, _settings.Token.TokenOwnerPublicKey);

            _blockhashPolicy = Policy
                .HandleResult<RequestResult<ResponseValue<LatestBlockHash>>>(x => !x.WasSuccessful || string.IsNullOrWhiteSpace(x.Result?.Value?.Blockhash))
                .RetryAsync(_settings.Polly.LatestBlockshashRetryCount, onRetry: async (response, retryCount) =>
                {
                    var result = response.Result;
                    Serilog.Log.Error("Attempt {0}: Failed to get latest blockhash from solana client. Reason: {1}, Details: {2}", retryCount, result.Reason, result.RawRpcResponse);
                    await Task.Delay(1000);
                });

            _getTransactionPolicy = Policy
                .HandleResult<RequestResult<TransactionMetaSlotInfo>>(x => !x.WasSuccessful)
                .RetryAsync(_settings.Polly.GetTransactionRetryCount, onRetry: async (response, retryCount) =>
                {
                    var result = response.Result;
                    Serilog.Log.Error("Attempt {0}: Failed to get data for the transaction. Reason: {1}, Details: {2}", retryCount, result.Reason, result.RawRpcResponse);
                    await Task.Delay(1000);
                });
        }

        private readonly MonkifyDbContext _context;
        private readonly IRpcClient _rpcClient;
        private readonly GeneralSettings _settings;

        private readonly Account _ownerAccount;

        private readonly AsyncRetryPolicy<RequestResult<ResponseValue<LatestBlockHash>>> _blockhashPolicy;
        private readonly AsyncRetryPolicy<RequestResult<TransactionMetaSlotInfo>> _getTransactionPolicy;


        public async Task<string?> GetLatestBlockhashForTokenTransfer()
            => await GetLatestBlockhashForTokenTransfer(Commitment.Confirmed);

        private async Task<string?> GetLatestBlockhashForTokenTransfer(Commitment commitment = Commitment.Confirmed)
        {
            try
            {
                var latestBlockHash = await _blockhashPolicy.ExecuteAsync(async () => await _rpcClient.GetLatestBlockHashAsync(commitment));

                if (latestBlockHash.WasSuccessful && !string.IsNullOrWhiteSpace(latestBlockHash.Result?.Value?.Blockhash))
                    return latestBlockHash.Result.Value.Blockhash;

                Serilog.Log.Error("Failed to get the latest Solana Blockhash. Details: {0}", latestBlockHash.RawRpcResponse);

                return null;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to get the latest Solana Blockhash.");
                return null;
            }
        }

        public async Task<bool> TransferTokensForBet(Bet bet, BetTransactionAmountResult amount)
        {
            if (amount.ValueInTokens == 0)
            {
                Serilog.Log.Warning("An attempt to make a transaction with 0 tokens has been made. Bet: {0}, ErrorMessage: {1}", bet.Id, amount.ErrorMessage);
                return true;
            }

            try
            {
                var result = await TransferTokens(bet.Wallet, amount);

                if (!result.WasSuccessful || string.IsNullOrWhiteSpace(result.Result))
                {
                    Serilog.Log.Error("Failed to transfer funds to the bet's wallet. Value: {1}. Details: {2} ", bet.Id, amount.AsJson());
                    return false;
                }

                await _context.TransactionLogs.AddAsync(new TransactionLog(bet.Id, amount.Value, result.Result));
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to reward the bet {0}. Value: {1}", bet.Id, amount.AsJson().ToString());
                return false;
            }
        }

        public async Task<bool> RefundTokens(string wallet, BetTransactionAmountResult amount)
        {
            if (amount.ValueInTokens == 0)
            {
                Serilog.Log.Warning("An attempt to refund a transaction with 0 tokens has been made. wallet, ErrorMessage: {1}", wallet, amount.ErrorMessage);
                return true;
            }

            try
            {
                var result = await TransferTokens(wallet, amount);

                if (!result.WasSuccessful || string.IsNullOrWhiteSpace(result.Result))
                {
                    Serilog.Log.Error("Failed to refund wallet {0}. Value: {1}. Details: {2} ", wallet, amount.AsJson(), result.RawRpcResponse);
                    return false;
                }

                await _context.Refunds.AddAsync(new RefundLog(wallet, amount.Value, result.Result));
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to refund the wallet {0}. Value: {1}", wallet, amount.AsJson().ToString());
                return false;
            }
        }

        public async Task<RequestResult<string>> TransferTokens(string wallet, BetTransactionAmountResult amount)
        {
            var latestBlockhash = await GetLatestBlockhashForTokenTransfer(Commitment.Finalized);

            if (string.IsNullOrWhiteSpace(latestBlockhash))
                return null;

            var tokenAccount = await _rpcClient.GetTokenAccountsByOwnerAsync(wallet, _settings.Token.MintAddress);

            if (!tokenAccount.WasSuccessful || tokenAccount.Result.Value == null || !tokenAccount.Result.Value.Any())
            {
                Serilog.Log.Error("Failed to transfer funds to wallet {0} due to it not having a token account for the token {1}. Value: {2}. Details: {3}", wallet, _settings.Token.MintAddress, amount.AsJson(), tokenAccount.RawRpcResponse);
                return null;
            }

            var transferInstruction = TokenProgram.Transfer(new PublicKey(_settings.Token.SenderAccount), new PublicKey(tokenAccount.Result.Value.FirstOrDefault().PublicKey), amount.ValueInTokens, _ownerAccount.PublicKey);

            var transaction = new TransactionBuilder()
                    .SetRecentBlockHash(latestBlockhash)
                    .SetFeePayer(_ownerAccount)
                    .AddInstruction(transferInstruction)
                    .Build(new List<Account> { _ownerAccount });

            return await _rpcClient.SendTransactionAsync(transaction);
        }

        public async Task<ValidationResult> ValidateBetPayment(Bet bet)
        {
            if (bet.Amount == 0)
            {
                Serilog.Log.Warning("An attempt to check for a paid transaction with 0 tokens has been made. Bet: {0}, Signature: {1}", bet.Id, bet.PaymentSignature);
                return new ValidationResult();
            }

            var transactionResponse = await _getTransactionPolicy.ExecuteAsync(async () => await _rpcClient.GetTransactionAsync(bet.PaymentSignature, Solnet.Rpc.Types.Commitment.Confirmed));

            if (!transactionResponse.WasSuccessful)
            {
                Serilog.Log.Error("Failed to validate payment signature {0}", bet.PaymentSignature);
                return new ValidationResult(ErrorMessages.InvalidPaymentSignature);
            }

            var balanceAccounts = GetAccountBalances(transactionResponse);
            var tokensBet = (ulong)(bet.Amount * (decimal)Math.Pow(10, _settings.Token.Decimals));

            var recipientErrorMessage = ValidateTokenExchangeForOwnerAccount(bet, tokensBet, balanceAccounts);

            if (!string.IsNullOrWhiteSpace(recipientErrorMessage))
                return new ValidationResult(recipientErrorMessage);

            var senderErrorMessage = ValidateTokenExchangeForSenderAccount(bet, tokensBet, balanceAccounts);

            if (!string.IsNullOrWhiteSpace(senderErrorMessage))
                return new ValidationResult(senderErrorMessage);

            return new ValidationResult();
        }

        private TransactionMetaData GetAccountBalances(RequestResult<TransactionMetaSlotInfo> transactionData)
        {
            var jsonBody = JObject.Parse(transactionData.RawRpcResponse);
            var metaData = jsonBody["result"]["meta"];
            var preBalances = metaData["preTokenBalances"].ToObject<List<AccountBalance>>();
            var postBalances = metaData["postTokenBalances"].ToObject<List<AccountBalance>>();

            return new TransactionMetaData(preBalances, postBalances);
        }

        private string? ValidateTokenExchangeForOwnerAccount(Bet bet, ulong tokensBet, TransactionMetaData transaction)
        {
            var recipientPreBalanceAccount = transaction.PreBalance.FirstOrDefault(x => x.Mint == _settings.Token.MintAddress && x.Owner == _settings.Token.TokenOwnerPublicKey);
            var recipientPostBalanceAccount = transaction.PostBalance.FirstOrDefault(x => x.Mint == _settings.Token.MintAddress && x.Owner == _settings.Token.TokenOwnerPublicKey);

            if (recipientPreBalanceAccount is null || recipientPostBalanceAccount is null)
                return ErrorMessages.SignatureWithoutOwnerAccount;

            var tokensExchanged = recipientPostBalanceAccount.UiTokenAmount.Amount - recipientPreBalanceAccount.UiTokenAmount.Amount;

            if (tokensExchanged == tokensBet)
                return null;

            Serilog.Log.Warning("An attempt to register a bet with invalid amounts for the token account was made. Bet data: {0}, Tokens exchanged: {1}", bet.AsJson(), tokensExchanged);

            return GenerateErrorMessageForTokenExchange(tokensExchanged, tokensBet);
        }

        private string? ValidateTokenExchangeForSenderAccount(Bet bet, ulong tokensBet, TransactionMetaData transaction)
        {
            var recipientPreBalanceAccount = transaction.PreBalance.FirstOrDefault(x => x.Mint == _settings.Token.MintAddress && x.Owner == bet.Wallet);
            var recipientPostBalanceAccount = transaction.PostBalance.FirstOrDefault(x => x.Mint == _settings.Token.MintAddress && x.Owner == bet.Wallet);

            if (recipientPreBalanceAccount is null || recipientPostBalanceAccount is null)
                return ErrorMessages.SignatureWithoutBetAccount;

            var tokensExchanged = recipientPreBalanceAccount.UiTokenAmount.Amount - recipientPostBalanceAccount.UiTokenAmount.Amount;

            if (tokensExchanged == tokensBet)
                return null;

            Serilog.Log.Warning("An attempt to register a bet with invalid amounts for the sender account was made. Bet data: {0}, Tokens exchanged: {1}", bet.AsJson(), tokensExchanged);

            return GenerateErrorMessageForTokenExchange(tokensExchanged, tokensBet);
        }

        private string GenerateErrorMessageForTokenExchange(ulong tokensExchanged, ulong tokensBet)
        {
            string? errorMessage;

            if (tokensExchanged < tokensBet)
                errorMessage = ErrorMessages.SignaturePaidLessThanBetAmount;
            else
                errorMessage = ErrorMessages.SignaturePaidMoreThanBetAmount;

            errorMessage += string.Format(ErrorMessages.AdviceOnDifferentSignatureBetAmount, _settings.Sessions.SupportEmail);

            return errorMessage;
        }
    }
}
