using MediatR;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Users.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers.Sessions.RegisterBet
{
    public class RegisterBetRequest : IRequest<bool>
    {
        public RegisterBetRequest(Guid sessionId, Guid userId, RegisterBetRequestBody body)
        {
            SessionId = sessionId;
            UserId = userId;
            Body = body;
        }

        public Guid SessionId { get; set; }
        public Guid UserId { get; set; }
        public RegisterBetRequestBody Body { get; set; }

        public Bet ToBet()
            => new()
            {
                SessionId = SessionId,
                UserId = UserId,
                Choice = Body.BetChoice,
                Amount = Body.BetAmount.Value
            };
    }
}
