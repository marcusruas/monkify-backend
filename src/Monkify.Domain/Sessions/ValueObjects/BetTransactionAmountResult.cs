using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.ValueObjects
{
    public record BetTransactionAmountResult
    {
        public BetTransactionAmountResult(decimal value, ulong valueInTokens)
        {
            Value = value;
            ValueInTokens = valueInTokens;
        }

        public BetTransactionAmountResult(string? errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public decimal Value { get; set; }
        public ulong ValueInTokens { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
