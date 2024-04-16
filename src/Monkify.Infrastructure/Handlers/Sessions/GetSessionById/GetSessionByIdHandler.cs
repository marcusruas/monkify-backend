using Microsoft.EntityFrameworkCore;
using Monkify.Common.Messaging;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.ResponseTypes.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers.Sessions.GetSessionById
{
    public class GetSessionByIdHandler : BaseRequestHandler<GetSessionByIdRequest, SessionDto?>
    {
        public GetSessionByIdHandler(MonkifyDbContext context, IMessaging messaging) : base(context, messaging) { }

        public override async Task<SessionDto?> HandleRequest(GetSessionByIdRequest request, CancellationToken cancellationToken)
        {
            var session = await Context.Sessions
                .Include(x => x.Parameters)
                .ThenInclude(x => x.PresetChoices)
                .Include(x => x.Bets)
                .FirstOrDefaultAsync(x => x.Id == request.SessionId);

            if (session is null)
                return null;

            return new SessionDto(session);
        }
    }
}
