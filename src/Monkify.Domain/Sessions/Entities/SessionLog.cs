using Monkify.Common.Models;
using Monkify.Domain.Sessions.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.Entities
{
    public class SessionLog : TableEntity
    {
        public SessionLog() { }

        public SessionLog(Guid sessionId, SessionStatus? previousStatus, SessionStatus newStatus)
        {
            SessionId = sessionId;
            PreviousStatus = previousStatus;
            NewStatus = newStatus;
        }

        public Session Session { get; set; }
        public Guid SessionId { get; set; }
        public SessionStatus? PreviousStatus { get; set; }
        public SessionStatus NewStatus { get; set; }
    }
}
