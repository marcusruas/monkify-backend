using Monkify.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.Entities
{
    public class TransactionLog : TableEntity
    {
        public TransactionLog() { }

        public TransactionLog(Guid betId, decimal amount, string signature)
        {
            BetId = betId;
            Amount = amount;
            Signature = signature;
        }

        public Bet Bet { get; set; }
        public Guid BetId { get; set; }
        public decimal Amount { get; set; }
        public string Signature { get; set; }
    }
}
