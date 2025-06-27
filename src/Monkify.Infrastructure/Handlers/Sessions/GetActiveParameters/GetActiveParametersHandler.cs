using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Monkify.Common.Notifications;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Contracts.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers.Sessions.GetActiveParameters
{
    public class GetActiveParametersHandler : BaseRequestHandler<GetActiveParametersRequest, IEnumerable<SessionParametersDto>>
    {
        public GetActiveParametersHandler(MonkifyDbContext context, INotifications messaging) : base(context, messaging) { }

        public override async Task<IEnumerable<SessionParametersDto>> HandleRequest(GetActiveParametersRequest request, CancellationToken cancellationToken)
        {
            var result = await Context.SessionParameters.Include(x => x.PresetChoices).Where(x => x.Active).ToListAsync();

            if (result.IsNullOrEmpty())
                return new List<SessionParametersDto>();

            return result.Select(x => new SessionParametersDto(x));
        }
    }
}
