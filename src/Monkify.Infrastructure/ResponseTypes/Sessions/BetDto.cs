using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;

namespace Monkify.Infrastructure.ResponseTypes.Sessions
{
    public class BetDto(Bet bet)
    {
        public Guid Id { get; set; } = bet.Id;
        public Guid SessionId { get; set; } = bet.SessionId;
        public string PaymentSignature { get; set; } = bet.PaymentSignature;
        public string Wallet { get; set; } = bet.Wallet;
        public string Choice { get; set; } = bet.Choice;
        public decimal Amount { get; set; } = bet.Amount;
        public BetStatus Status { get; set; } = bet.Status;
        public DateTime Date { get; set; } = bet.CreatedDate;
    }
}
