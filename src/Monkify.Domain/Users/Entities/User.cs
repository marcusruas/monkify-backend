using Bogus;
using Monkify.Common.Models;
using Monkify.Domain.Monkey.Entities;
using Monkify.Domain.Users.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Users.Entities
{
    public class User : TableEntity
    {
        public User()
        {
            var bogus = new Faker();
            Username = $"{bogus.Internet.UserName()}{bogus.Random.Int(100, 500)}";
            Active = true;
        }

        public string Username { get; set; }
        public string Email { get; set; }
        public string WalletId { get; set; }
        public bool Active { get; set; }
        public ICollection<Bet> Bets { get; set; }
    }
}
