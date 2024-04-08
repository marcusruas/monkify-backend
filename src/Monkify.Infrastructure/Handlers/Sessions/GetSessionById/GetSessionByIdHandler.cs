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
    public class GetSessionByIdHandler : BaseRequestHandler<GetSessionByIdRequest, SessionDto>
    {
        public GetSessionByIdHandler(MonkifyDbContext context, IMessaging messaging) : base(context, messaging) { }

        public override async Task<SessionDto> HandleRequest(GetSessionByIdRequest request, CancellationToken cancellationToken)
        {
            var session = await Context.Sessions.Include(x => x.Parameters).Include(x => x.Bets).ThenInclude(x => x.User).FirstOrDefaultAsync(x => x.Id == request.SessionId);

            if (session is null)
                Messaging.ReturnValidationFailureMessage("Session was not found.");

            return new SessionDto(session);
        }
    }
}
