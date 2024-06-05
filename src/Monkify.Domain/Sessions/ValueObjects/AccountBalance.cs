using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.ValueObjects
{
    public class TransactionMetaData
    {
        public TransactionMetaData(List<AccountBalance> preBalances, List<AccountBalance> postBalances)
        {
            PreBalance = preBalances;
            PostBalance = postBalances;
        }

        public List<AccountBalance> PreBalance { get; set; }
        public List<AccountBalance> PostBalance { get; set; }
    }

    public class AccountBalance
    {
        public string Mint { get; set; }
        public string Owner { get; set; }
        public AccountBalanceAmount UiTokenAmount { get; set; }
    }

    public class AccountBalanceAmount
    {
        public ulong Amount { get; set; }
        public int Decimals { get; set; }
    }
}
