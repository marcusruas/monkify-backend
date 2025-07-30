using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Consumers.BetPlaced
{
    public record BetPlacedEvent(Guid SessionId) { }
}
