using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monkify.Common.Models;

namespace Monkify.Domain.Sessions.Entities
{
    public class RefundLog : TableEntity
    {
        public RefundLog() { }

        public RefundLog(string wallet, decimal amount, string signature)
        {
            Wallet = wallet;
            Amount = amount;
            Signature = signature;
        }

        public string Wallet { get; set; }
        public decimal Amount { get; set; }
        public string Signature { get; set; }
    }
}
