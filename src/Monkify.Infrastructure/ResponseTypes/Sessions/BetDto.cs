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
            Amount = bet.Amount;
            Choice = bet.Choice;
            User = bet.User.Username;
            UserId = bet.UserId;
            Won = bet.Won;
            Refunded = bet.Refunded;
        }

        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Choice { get; set; }
        public string User { get; set; }
        public Guid UserId { get; set; }
        public bool Won { get; set; }
        public bool Refunded { get; set; }
    }
}
