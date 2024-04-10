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
        public Bet Bet { get; set; }
        public Guid BetId { get; set; }
        public double Amount { get; set; }
        public string Wallet { get; set; }
        public string Signature { get; set; }
    }
}
