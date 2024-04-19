using Monkify.Common.Results;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Services.Solana
{
    public interface ISolanaService
    {
        Task<bool> SetLatestBlockhashForTokenTransfer();
        Task<bool> TransferTokensForBet(Bet bet, BetTransactionAmountResult amount);
        Task<ValidationResult> ValidateBetPayment(Bet bet);
    }
}
