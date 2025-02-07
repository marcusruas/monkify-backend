using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.ResponseTypes.Sessions
{
    public class SessionParametersDto(SessionParameters parameters)
    {
        public Guid SessionTypeId { get; set; } = parameters.Id;
        public SessionCharacterType SessionCharacterType { get; set; } = parameters.AllowedCharacters;
        public string Name { get; set; } = parameters.Name;
        public decimal RequiredAmount { get; set; } = parameters.RequiredAmount;
        public int MinimumNumberOfPlayers { get; set; } = parameters.MinimumNumberOfPlayers;
        public int? ChoiceRequiredLength { get; set; } = parameters.ChoiceRequiredLength;
        public bool PlayersDefineCharacters { get; set; } = parameters.PlayersDefineCharacters;
        public bool AcceptDuplicatedCharacters { get; set; } = parameters.AcceptDuplicatedCharacters;
        public IEnumerable<string>? PresetChoices { get; set; } = !parameters.PresetChoices.IsNullOrEmpty() ? parameters.PresetChoices.Select(x => x.Choice) : new List<string>();
    }
}
