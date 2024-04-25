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
            Status = WaitingBets;
            StatusLogs = new Collection<SessionStatusLog>() { new SessionStatusLog(Id, null, SessionStatus.WaitingBets) };
            Bets = new Collection<Bet>();
        }

        public Session(Guid parametersId) : base()
        {
            ParametersId = parametersId;
        }

        public SessionParameters Parameters { get; set; }
        public Guid ParametersId { get; set; }
        public SessionStatus Status { get; set; }
        public DateTime? EndDate { get; set; }
        public string? WinningChoice { get; set; }
        public int? Seed { get; set; }
        public ICollection<Bet> Bets { get; set; }
        public ICollection<SessionStatusLog> StatusLogs { get; set; }

        public static SessionStatus[] SessionInProgressStatus = [WaitingBets, Started];
        public static SessionStatus[] SessionEndedStatus = [NotEnoughPlayersToStart, Ended];
    }
}
