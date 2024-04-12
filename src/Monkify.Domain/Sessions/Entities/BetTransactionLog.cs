using Monkify.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.Entities
{
    public class BetTransactionLog : TableEntity
    {
        public BetTransactionLog() { }

        public BetTransactionLog(decimal amount, string signature, Guid betId)
        {
            Amount = amount;
            Signature = signature;
            BetId = betId;
        }

        public decimal Amount { get; set; }
        public string Signature { get; set; }
        public Bet Bet { get; set; }
        public Guid BetId { get; set; }
    }
}
