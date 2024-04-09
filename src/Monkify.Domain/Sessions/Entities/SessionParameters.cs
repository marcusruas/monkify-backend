using Monkify.Common.Models;
using Monkify.Domain.Sessions.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.Entities
{
    public class SessionParameters : TableEntity
    {
        public SessionCharacterType SessionCharacterType { get; set; }
        public double RequiredAmount { get; set; }
        public int MinimumNumberOfPlayers { get; set; }
        public int ChoiceRequiredLength { get; set; }
        public bool AcceptDuplicatedCharacters { get; set; }
        public bool Active { get; set; }
    }
}
