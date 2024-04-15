using MediatR;
using Microsoft.IdentityModel.Tokens;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.Events
{
    public class SessionCreated : INotification
    {
        public SessionCreated(Guid sessionId, SessionParameters parameters)
        {
            SessionId = sessionId;
            CharacterType = parameters.SessionCharacterType;
            MinimumNumberOfPlayers = parameters.MinimumNumberOfPlayers;
            ChoiceRequiredLength = parameters.ChoiceRequiredLength;

            if (!parameters.PresetChoices.IsNullOrEmpty())
                PresetChoices = parameters.PresetChoices.Select(x => x.Choice).ToList();
        }

        public Guid SessionId { get; set; }
        public SessionCharacterType CharacterType { get; set; }
        public int MinimumNumberOfPlayers { get; set; }
        public int? ChoiceRequiredLength { get; set; }
        public List<string> PresetChoices { get; set; }
    }
}
