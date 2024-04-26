using MediatR;
using Monkify.Infrastructure.ResponseTypes.Sessions;

namespace Monkify.Infrastructure.Handlers.Sessions.GetActiveParameters
{
    public class GetActiveParametersRequest : IRequest<IEnumerable<SessionParametersDto>> { }
}
