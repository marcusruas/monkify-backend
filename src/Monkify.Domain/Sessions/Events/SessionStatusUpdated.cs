using Monkify.Domain.Sessions.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.Events
{
    public record SessionStatusUpdated(SessionStatus Status, DateTime? StartDate, SessionResult? Result = null);
}
