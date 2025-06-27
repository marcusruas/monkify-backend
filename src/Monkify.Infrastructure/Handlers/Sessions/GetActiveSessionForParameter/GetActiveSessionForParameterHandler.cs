using Microsoft.EntityFrameworkCore;
using Monkify.Common.Notifications;
using Monkify.Common.Resources;
using Monkify.Domain.Sessions.Entities;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Contracts.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers.Sessions.GetActiveSessionForParameter
{
    public class GetActiveSessionForParameterHandler : BaseRequestHandler<GetActiveSessionForParameterRequest, ActiveSessionDto>
    {
        public GetActiveSessionForParameterHandler(MonkifyDbContext context, INotifications messaging) : base(context, messaging)
        {
        }

        public override async Task<ActiveSessionDto> HandleRequest(GetActiveSessionForParameterRequest request, CancellationToken cancellationToken)
        {
            await ValidateParameter(request);
            var existingSession = await GetActiveSession(request);

            return new ActiveSessionDto(existingSession);
        }

        private async Task ValidateParameter(GetActiveSessionForParameterRequest request)
        {
            bool parameterIsValid = await Context.SessionParameters
                .AnyAsync(x => x.Id == request.ParameterId && x.Active);

            if (!parameterIsValid)
                Messaging.ReturnValidationFailureNotification(ErrorMessages.ParameterNotFound);
        }

        private async Task<Session> GetActiveSession(GetActiveSessionForParameterRequest request)
        {
            var result = await Context.Sessions
                .Include(x => x.Bets)
                .FirstOrDefaultAsync(x => x.ParametersId == request.ParameterId && Session.SessionInProgressStatus.Contains(x.Status));

            if (result is null)
                Messaging.ReturnValidationFailureNotification(ErrorMessages.ParameterHasNoActiveSessions);

            return result;
        }
    }
}
