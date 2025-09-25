using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Monkify.Domain.Sessions.Entities;
using Monkify.Infrastructure.Abstractions;

namespace Monkify.Infrastructure.Background.Events.BetPlaced
{
    public record BetPlacedEvent(Bet Bet) : INotification { }
}
