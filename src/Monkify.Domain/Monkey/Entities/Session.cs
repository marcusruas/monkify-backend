using Monkify.Common.Models;
using Monkify.Domain.Monkey.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Monkey.Entities
{
    public class Session : TableEntity
    {
        public Session() { }
        public Session(SessionCharacterType sessionCharacterType)
        {
            SessionCharacterType = sessionCharacterType;
            Active = true;
        }

        public SessionCharacterType SessionCharacterType { get; set; }
        public bool HasWinner { get; set; }
        public bool Active { get; set; }
    }
}
