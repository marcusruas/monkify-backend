using Monkify.Common.Resources;
using Monkify.Common.Results;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Context;
using Polly;
using Polly.Retry;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Core.Http;
using Solnet.Rpc.Messages;
using Solnet.Rpc.Models;
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

            _ownerAccount ??= new Account(Convert.FromBase64String(_settings.Token.TokenOwnerPrivateKey), new PublicKey(_settings.Token.TokenOwnerPublicKey).KeyBytes);

            _blockhashPolicy = Policy
                .HandleResult<RequestResult<ResponseValue<LatestBlockHash>>>(x => !x.WasHttpRequestSuccessful || string.IsNullOrWhiteSpace(x.Result?.Value?.Blockhash))
                .RetryAsync(_settings.Polly.LatestBlockshashRetryCount, onRetry: (response, retryCount) =>
                {
                    var result = response.Result;
                    var test = retryCount;
                    Serilog.Log.Error("Attempt {0}: Failed to get latest blockhash from solana client. Reason: {1}, Details: {2}", retryCount, result.Reason, result.RawRpcResponse);
                });

            _getTransactionPolicy = Policy
                .HandleResult<RequestResult<TransactionMetaSlotInfo>>(x => !x.WasHttpRequestSuccessful || !x.WasRequestSuccessfullyHandled)
                .RetryAsync(_settings.Polly.GetTransactionRetryCount, onRetry: (response, retryCount) =>
                {
                    var result = response.Result;
                    Serilog.Log.Error("Attempt {0}: Failed to get data for the transaction. Reason: {1}, Details: {2}", retryCount, result.Reason, result.RawRpcResponse);
                });
        }

        private readonly MonkifyDbContext _context;
        private readonly IRpcClient _rpcClient;
        private readonly GeneralSettings _settings;

        private readonly Account _ownerAccount;
        private string _latestBlockhashAddress;

        private readonly AsyncRetryPolicy<RequestResult<ResponseValue<LatestBlockHash>>> _blockhashPolicy;
        private readonly AsyncRetryPolicy<RequestResult<TransactionMetaSlotInfo>> _getTransactionPolicy;

        public async Task<bool> SetLatestBlockhashForTokenTransfer()
        {
            var latestBlockHash = await _blockhashPolicy.ExecuteAsync(async () => await _rpcClient.GetLatestBlockHashAsync());
            bool blockhhashObtained = latestBlockHash.WasHttpRequestSuccessful && !string.IsNullOrWhiteSpace(latestBlockHash.Result?.Value?.Blockhash);

            if (blockhhashObtained)
                _latestBlockhashAddress = latestBlockHash.Result.Value.Blockhash;

            return blockhhashObtained;
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
                var transferInstruction = TokenProgram.Transfer(new PublicKey(_settings.Token.SenderAccount), new PublicKey(bet.Wallet), amount.ValueInTokens, _ownerAccount.PublicKey);

                var transaction = new TransactionBuilder()
                        .SetRecentBlockHash(_latestBlockhashAddress)
                        .SetFeePayer(_ownerAccount)
                        .AddInstruction(transferInstruction)
                        .Build(new List<Account> { _ownerAccount });

                RequestResult<string> result = await _rpcClient.SendTransactionAsync(transaction);

                if (!result.WasHttpRequestSuccessful || !result.WasRequestSuccessfullyHandled)
                {
                    Serilog.Log.Error("Failed to transfer funds to the bet's wallet. Value: {1}. Details: {2} ", bet.Id, amount.AsJson(), result.RawRpcResponse);
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

        public async Task<ValidationResult> ValidateBetPayment(Bet bet)
        {
            if (bet.Amount == 0)
            {
                Serilog.Log.Warning("An attempt to check for a paid transaction with 0 tokens has been made. Bet: {0}, Signature: {1}", bet.Id, bet.PaymentSignature);
                return new ValidationResult();
            }

            var transactionResponse = await _getTransactionPolicy.ExecuteAsync(async () => await _rpcClient.GetTransactionAsync(bet.PaymentSignature));

            if (transactionResponse.WasSuccessful)
                return new ValidationResult(ErrorMessages.InvalidPaymentSignature);

            var tokensBet = (ulong)(bet.Amount * (decimal)Math.Pow(10, _settings.Token.Decimals));

            var recipientErrorMessage = ValidateTokenExchangeForOwnerAccount(bet, tokensBet, transactionResponse.Result);

            if (!string.IsNullOrWhiteSpace(recipientErrorMessage))
                return new ValidationResult(recipientErrorMessage);

            var senderErrorMessage = ValidateTokenExchangeForSenderAccount(bet, tokensBet, transactionResponse.Result);

            if (!string.IsNullOrWhiteSpace(senderErrorMessage))
                return new ValidationResult(senderErrorMessage);

            return new ValidationResult();
        }

        private string? ValidateTokenExchangeForOwnerAccount(Bet bet, ulong tokensBet, TransactionMetaSlotInfo transaction)
        {
            var tokenOwnerIndex = Array.IndexOf(transaction.Transaction.Message.AccountKeys, _settings.Token.SenderAccount);

            if (tokenOwnerIndex == -1)
                return ErrorMessages.SignatureWithoutOwnerAccount;

            var recipientPreBalanceAccount = transaction.Meta.PreTokenBalances.FirstOrDefault(x => x.Mint == _settings.Token.MintAddress && x.AccountIndex == tokenOwnerIndex);
            var recipientPostBalanceAccount = transaction.Meta.PostTokenBalances.FirstOrDefault(x => x.Mint == _settings.Token.MintAddress && x.AccountIndex == tokenOwnerIndex);

            var tokensExchanged = recipientPostBalanceAccount.UiTokenAmount.AmountUlong - recipientPreBalanceAccount.UiTokenAmount.AmountUlong;

            if (tokensExchanged == tokensBet)
                return null;

            Serilog.Log.Warning("An attempt to register a bet with invalid amounts for the token account was made. Bet data: {0}, Tokens exchanged: {1}", bet.AsJson(), tokensExchanged);

            return GenerateErrorMessageForTokenExchange(tokensExchanged, tokensBet);
        }

        private string? ValidateTokenExchangeForSenderAccount(Bet bet, ulong tokensBet, TransactionMetaSlotInfo transaction)
        {
            var senderIndex = Array.IndexOf(transaction.Transaction.Message.AccountKeys, bet.Wallet);

            if (senderIndex == -1)
                return ErrorMessages.SignatureWithoutBetAccount;

            var senderPreBalanceAccount = transaction.Meta.PreTokenBalances.FirstOrDefault(x => x.Mint == _settings.Token.MintAddress && x.AccountIndex == senderIndex);
            var senderPostBalanceAccount = transaction.Meta.PostTokenBalances.FirstOrDefault(x => x.Mint == _settings.Token.MintAddress && x.AccountIndex == senderIndex);

            var tokensExchanged = senderPreBalanceAccount.UiTokenAmount.AmountUlong - senderPostBalanceAccount.UiTokenAmount.AmountUlong;

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

            errorMessage += ErrorMessages.AdviceOnDifferentSignatureBetAmount;

            return errorMessage;
        }
    }
}
