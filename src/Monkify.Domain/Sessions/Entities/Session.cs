using MediatR;
using Monkify.Common.Models;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Monkify.Domain.Sessions.ValueObjects.SessionStatus;

namespace Monkify.Domain.Sessions.Entities
{
    public class Session : TableEntity
    {
        public Session() { }

        public Session(Guid parametersId)
        {
            ParametersId = parametersId;
            Status = WaitingBets;
        }

        public SessionStatus Status { get; set; }
        public DateTime? EndDate { get; set; }
        public SessionParameters Parameters { get; set; }
        public Guid ParametersId { get; set; }
        public ICollection<Bet> Bets { get; set; }
        public ICollection<SessionLog> Logs { get; set; }

        public void UpdateStatus(SessionStatus status)
        {
            Status = status;

            if (SessionEndedStatus.Contains(status))
                EndDate = DateTime.UtcNow;
        }

        public static SessionStatus[] SessionInProgressStatus = { WaitingBets, Started };
        public static SessionStatus[] SessionEndedStatus = { NotEnoughPlayersToStart, Ended };
    }
}
