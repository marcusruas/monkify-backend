using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Users.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.ResponseTypes.Sessions
{
    public class BetDto
    {
        public BetDto(Bet bet)
        {
            Id = bet.Id;
            BetAmount = bet.BetAmount;
            BetChoice = bet.BetChoice;
            UserId = bet.UserId;
            Won = bet.Won;
            Refunded = bet.Refunded;
        }

        public Guid Id { get; set; }
        public double BetAmount { get; set; }
        public string BetChoice { get; set; }
        public Guid UserId { get; set; }
        public bool Won { get; set; }
        public bool Refunded { get; set; }
    }
}
