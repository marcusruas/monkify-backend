using MediatR;
using Monkify.Domain.Sessions.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.Events
{
    public record SessionStartEvent(Session Session) : INotification;
}
