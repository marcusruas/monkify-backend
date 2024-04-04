using Monkify.Common.Models;
using Monkify.Domain.Users.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.Entities
{
    public class Bet : TableEntity
    {
        public double BetAmount { get; set; }
        public string BetChoice { get; set; }
        public User User { get; set; }
        public Guid UserId { get; set; }
        public Session Session { get; set; }
        public Guid SessionId { get; set; }
        public bool Won { get; set; }
        public bool Refunded { get; set; }
    }
}
