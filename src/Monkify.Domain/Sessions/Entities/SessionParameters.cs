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
        public SessionParameters() { }
        public SessionParameters(SessionCharacterType allowedCharacters, int choiceRequiredLength, bool acceptDuplicateCharacters)
        {
            AllowedCharacters = allowedCharacters;
            ChoiceRequiredLength = choiceRequiredLength;
            AcceptDuplicatedCharacters = acceptDuplicateCharacters;
        }

        public SessionCharacterType AllowedCharacters { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal RequiredAmount { get; set; }
        public int MinimumNumberOfPlayers { get; set; }
        public int? ChoiceRequiredLength { get; set; }
        public bool AcceptDuplicatedCharacters { get; set; }
        public bool Active { get; set; }
        public ICollection<PresetChoice> PresetChoices { get; set; }
        public ICollection<Session> Sessions { get; set; }
    }
}
