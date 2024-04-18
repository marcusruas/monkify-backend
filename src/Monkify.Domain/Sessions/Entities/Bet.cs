using Monkify.Common.Models;
using Monkify.Domain.Sessions.ValueObjects;

namespace Monkify.Domain.Sessions.Entities
{
    public class Bet : TableEntity
    {
        public Bet() { }
        public Bet(Guid sessionId, string wallet, string choice, decimal amount)
        {
            SessionId = sessionId;
            Wallet = wallet;
            Choice = choice;
            Amount = amount;
            StatusLogs = new List<BetStatusLog>() { new (Id, null, BetPaymentStatus.NotApplicable) };
        }

        public Session Session { get; set; }
        public Guid SessionId { get; set; }
        public string Wallet { get; set; }
        public string Choice { get; set; }
        public decimal Amount { get; set; }
        public BetPaymentStatus PaymentStatus { get; set; }
        public ICollection<BetStatusLog> StatusLogs { get; set; }
        public ICollection<TransactionLog> TransactionLogs { get; set; }
    }
}
