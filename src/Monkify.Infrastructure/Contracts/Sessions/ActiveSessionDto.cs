using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Contracts.Sessions
{
    public class ActiveSessionDto(Session session)
    {
        public Guid SessionId { get; set; } = session.Id;
        public SessionStatus Status { get; set; } = session.Status;
        public string? WinningChoice { get; set; } = session.WinningChoice;
        public IEnumerable<BetDto> Bets { get; set; } = !session.Bets.IsNullOrEmpty() ? session.Bets.Select(x => new BetDto(x)) : new List<BetDto>();
    }
}
