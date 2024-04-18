using Monkify.Common.Models;
using Monkify.Domain.Sessions.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.Entities
{
    public class BetStatusLog : TableEntity
    {
        public BetStatusLog() { }

        public BetStatusLog(Guid betId, BetStatus? previousStatus, BetStatus newStatus)
        {
            BetId = betId;
            PreviousStatus = previousStatus;
            NewStatus = newStatus;
        }

        public Bet Bet { get; set; }
        public Guid BetId { get; set; }
        public BetStatus? PreviousStatus { get; set; }
        public BetStatus NewStatus { get; set; }
    }
}
