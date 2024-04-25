using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;

namespace Monkify.Infrastructure.ResponseTypes.Sessions
{
    public class BetDto(Bet bet)
    {
        public string PaymentSignature { get; set; } = bet.PaymentSignature;
        public string Choice { get; set; } = bet.Choice;
        public decimal Amount { get; set; } = bet.Amount;
        public bool Won { get; set; } = bet.Status == BetStatus.NeedsRewarding || bet.Status == BetStatus.Rewarded;
        public DateTime Date { get; set; } = bet.CreatedDate;
    }
}
