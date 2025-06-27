using MediatR;
using Monkify.Infrastructure.Contracts.Sessions;

namespace Monkify.Infrastructure.Handlers.Sessions.GetActiveParameters
{
    public class GetActiveParametersRequest : IRequest<IEnumerable<SessionParametersDto>> { }
}
