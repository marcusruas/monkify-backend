using Microsoft.IdentityModel.Tokens;
using Monkify.Domain.Sessions.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.ResponseTypes.Sessions
{
    public class SessionDto
    {
        public SessionDto(Session session)
        {
            Id = session.Id;
            Active = session.Active;
            StartDate = session.CreatedDate;
            EndDate = session.EndDate;
            Bets = new();

            if (session.Parameters != null)
                Parameters = new SessionParametersDto(session.Parameters);

            if (!session.Bets.IsNullOrEmpty())
                Bets.AddRange(session.Bets.Select(x => new BetDto(x)));
        }

        public Guid Id { get; set; }
        public bool Active { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public SessionParametersDto Parameters { get; set; }
        public List<BetDto> Bets { get; set; }
    }
}
