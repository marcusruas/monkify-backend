using MediatR;
using Monkify.Domain.Monkey.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Monkey.Events
{
    public class SessionCreated : INotification
    {
        public SessionCreated(Guid sessionId, SessionCharacterType sessionCharacterType, int minimumNumberOfPlayers)
        {
            SessionId = sessionId;
            SessionCharacterType = sessionCharacterType;
            MinimumNumberOfPlayers = minimumNumberOfPlayers;

        }

        public Guid SessionId { get; set; }
        public SessionCharacterType SessionCharacterType { get; set; }
        public int MinimumNumberOfPlayers { get; set; }
    }
}
