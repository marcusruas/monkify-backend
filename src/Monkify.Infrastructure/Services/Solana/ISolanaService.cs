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
        Task<bool> TransferTokens(Guid betId, string walletId, BetTransactionAmountResult amount);
        Task<bool> TransferRefundTokens(Bet bet, BetTransactionAmountResult amount);
    }
}
