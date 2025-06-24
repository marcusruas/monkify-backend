using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using Monkify.Domain.Sessions.Events;

namespace Monkify.Infrastructure.Background.Events
{
    public class BetPlacedConsumer : IConsumer<BetPlacedEvent>
    {
        public Task Consume(ConsumeContext<BetPlacedEvent> context)
        {
            throw new NotImplementedException();
        }
    }
}
