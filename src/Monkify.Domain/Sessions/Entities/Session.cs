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
        public Session()
        {
            Logs = new Collection<SessionLog>();
        }

        public Session(Guid parametersId)
        {
            ParametersId = parametersId;
            Status = WaitingBets;

            Logs = new Collection<SessionLog>
            {
                new SessionLog(Id, null, WaitingBets)
            };
        }

        public SessionParameters Parameters { get; set; }
        public Guid ParametersId { get; set; }
        public SessionStatus Status { get; set; }
        public DateTime? EndDate { get; set; }
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
