using Monkify.Common.Models;
using Monkify.Domain.Sessions.ValueObjects;

namespace Monkify.Domain.Sessions.Entities
{
    public class Bet : TableEntity
    {
        public Bet() { }

        public Bet(BetStatus status, decimal amount)
        {
            Status = status;
            Amount = amount;
        }

        public Bet(Guid sessionId, string seed, string paymentSignature, string wallet, string choice, decimal amount)
        {
            SessionId = sessionId;
            Seed = seed;
            PaymentSignature = paymentSignature;
            Wallet = wallet;
            Choice = choice;
            Amount = amount;
            StatusLogs = new List<BetStatusLog>() { new (Id, null, BetStatus.NotApplicable) };
        }

        public Session Session { get; set; }
        public Guid SessionId { get; set; }
        public string Seed { get; set; }
        public string PaymentSignature { get; set; }
        public string Wallet { get; set; }
        public string Choice { get; set; }
        public decimal Amount { get; set; }
        public BetStatus Status { get; set; }
        public ICollection<BetStatusLog> StatusLogs { get; set; }
        public ICollection<TransactionLog> TransactionLogs { get; set; }
    }
}
