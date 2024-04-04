using MediatR;
using Monkify.Common.Models;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.Entities
{
    public class Session : TableEntity
    {
        public Session(Guid parametersId)
        {
            ParametersId = parametersId;
            Active = true;
        }

        public bool Active { get; set; }
        public DateTime? EndDate { get; set; }
        public SessionParameters Parameters { get; set; }
        public Guid ParametersId { get; set; }
        public ICollection<Bet> Bets { get; set; }
    }
}
