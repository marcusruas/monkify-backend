using Monkify.Common.Models;

namespace Monkify.Domain.Sessions.Entities
{
    public class Bet : TableEntity
    {
        public string Wallet { get; set; }
        public string Choice { get; set; }
        public decimal Amount { get; set; }
        public Session Session { get; set; }
        public Guid SessionId { get; set; }
        public bool Won { get; set; }
        public bool Refunded { get; set; }
        public ICollection<BetTransactionLog> Logs { get; set; }
    }
}
