using MediatR;
using Monkify.Common.Models;
using Monkify.Domain.Monkey.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Monkey.Entities
{
    public class Session : TableEntity
    {
        public Session(Guid parametersId)
        {
            ParametersId = parametersId;
            Active = true;
        }

        public bool HasWinner { get; set; }
        public bool Active { get; set; }
        public SessionParameters Parameters { get; set; }
        public Guid ParametersId { get; set; }
        public ICollection<Bet> Bets { get; set; }
    }
}
