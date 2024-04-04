using Microsoft.EntityFrameworkCore;
using Monkify.Common.Messaging;
using Monkify.Domain.Sessions.Entities;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.ResponseTypes.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers.Sessions.GetActiveSessions
{
    public class GetActiveSessionsHandler : BaseRequestHandler<GetActiveSessionsRequest, IEnumerable<SessionDto>>
    {
        public GetActiveSessionsHandler(MonkifyDbContext context, IMessaging messaging) : base(context, messaging) { }

        public override async Task<IEnumerable<SessionDto>> HandleRequest(GetActiveSessionsRequest request, CancellationToken cancellationToken)
        {
            var activeSessions = await Context.Sessions.Include(x => x.Parameters).Include(x => x.Bets).Where(x => Session.SessionInProgressStatus.Contains(x.Status)).ToListAsync();
            return activeSessions.Select(x => new SessionDto(x));
        }
    }
}
