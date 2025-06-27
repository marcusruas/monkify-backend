using MediatR;
using Monkify.Infrastructure.Contracts.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers.Sessions.GetActiveSessionForParameter
{
    public record GetActiveSessionForParameterRequest(Guid ParameterId) : IRequest<ActiveSessionDto> { }
}
