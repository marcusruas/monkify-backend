using Monkify.Domain.Sessions.Entities;

namespace Monkify.Infrastructure.ResponseTypes.Sessions
{
    public class BetDto(Bet bet)
    {
        public Guid Id { get; set; } = bet.Id;
        public string Wallet { get; set; } = bet.Wallet;
        public string Choice { get; set; } = bet.Choice;
        public decimal Amount { get; set; } = bet.Amount;
        public bool Won { get; set; } = bet.Won;
        public bool Refunded { get; set; } = bet.Refunded;
        public Guid SessionId { get; set; } = bet.SessionId;
        public DateTime Date { get; set; } = bet.CreatedDate;
    }
}
