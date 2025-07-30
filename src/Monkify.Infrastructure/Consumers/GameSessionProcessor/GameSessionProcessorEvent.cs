using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monkify.Domain.Sessions.Entities;

namespace Monkify.Infrastructure.Consumers.GameSessionProcessor
{
    public record GameSessionProcessorEvent(Session Session) { }
}
