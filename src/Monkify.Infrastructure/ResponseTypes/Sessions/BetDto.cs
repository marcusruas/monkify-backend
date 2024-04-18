using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;

namespace Monkify.Infrastructure.ResponseTypes.Sessions
{
    public class BetDto(Bet bet)
    {
        public Guid Id { get; set; } = bet.Id;
        public Guid SessionId { get; set; } = bet.SessionId;
        public string Wallet { get; set; } = bet.Wallet;
        public string Choice { get; set; } = bet.Choice;
        public decimal Amount { get; set; } = bet.Amount;
        public BetPaymentStatus Won { get; set; } = bet.PaymentStatus;
        public DateTime Date { get; set; } = bet.CreatedDate;
    }
}
