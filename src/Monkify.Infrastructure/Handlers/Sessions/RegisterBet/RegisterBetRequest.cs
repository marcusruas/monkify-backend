using MediatR;
using Monkify.Domain.Sessions.Entities;
using Monkify.Infrastructure.ResponseTypes.Sessions;

namespace Monkify.Infrastructure.Handlers.Sessions.RegisterBet
{
    public class RegisterBetRequest : IRequest<BetDto>
    {
        public RegisterBetRequest(Guid sessionId, RegisterBetRequestBody body)
        {
            SessionId = sessionId;
            Body = body;
        }

        public Guid SessionId { get; set; }
        public RegisterBetRequestBody Body { get; set; }

        public Bet ToBet()
            => new()
            {
                SessionId = SessionId,
                Wallet = Body.Wallet,
                Choice = Body.Choice,
                Amount = Body.Amount.Value
            };
    }
}
