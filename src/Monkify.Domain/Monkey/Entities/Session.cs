using MediatR;
using Monkify.Common.Models;
using Monkify.Domain.Monkey.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Monkey.Entities
{
    public class Session : TableEntity, INotification
    {
        public Session() { }
        public Session(SessionCharacterType sessionCharacterType)
        {
            CharacterType = sessionCharacterType;
            Active = true;
        }

        public SessionCharacterType CharacterType { get; set; }
        public bool HasWinner { get; set; }
        public bool Active { get; set; }
        public ICollection<Bet> Bets { get; set; }
    }
}
