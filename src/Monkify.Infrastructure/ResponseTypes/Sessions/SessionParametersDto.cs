using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.ResponseTypes.Sessions
{
    public class SessionParametersDto
    {
        public SessionParametersDto(SessionParameters parameters)
        {
            Id = parameters.Id;
            RequiredAmount = parameters.RequiredAmount;
            SessionCharacterType = parameters.SessionCharacterType;
            MinimumNumberOfPlayers = parameters.MinimumNumberOfPlayers;
            ChoiceRequiredLength = parameters.ChoiceRequiredLength;
            AcceptDuplicatedCharacters = parameters.AcceptDuplicatedCharacters;
            Active = parameters.Active;
        }

        public Guid Id { get; set; }
        public SessionCharacterType SessionCharacterType { get; set; }
        public decimal RequiredAmount { get; set; }
        public int MinimumNumberOfPlayers { get; set; }
        public int ChoiceRequiredLength { get; set; }
        public bool AcceptDuplicatedCharacters { get; set; }
        public bool Active { get; set; }
    }
}
