using MediatR;
using Monkify.Infrastructure.ResponseTypes.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers.Sessions.GetSessionById
{
    public record GetSessionByIdRequest(Guid SessionId) : IRequest<SessionDto?>;
}
