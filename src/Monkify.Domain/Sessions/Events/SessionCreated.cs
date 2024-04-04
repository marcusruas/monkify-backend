using MediatR;
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
        }

        public Guid SessionId { get; set; }
        public SessionCharacterType CharacterType { get; set; }
        public int MinimumNumberOfPlayers { get; set; }
        public int ChoiceRequiredLength { get; set; }
    }
}
