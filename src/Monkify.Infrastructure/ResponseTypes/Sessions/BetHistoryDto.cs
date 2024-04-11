using Monkify.Domain.Sessions.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.ResponseTypes.Sessions
{
    public class BetHistoryDto
    {
        public BetHistoryDto(Bet bet)
        {
            Id = bet.Id;
            Username = bet.User.Username;
            Choice = bet.Choice;
            Amount = bet.Amount;
            Date = bet.CreatedDate;
            Won = bet.Won;
        }

        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Choice { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public bool Won { get; set; }
    }
}
