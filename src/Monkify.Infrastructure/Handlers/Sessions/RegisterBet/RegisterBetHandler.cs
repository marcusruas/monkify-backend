using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Monkify.Common.Messaging;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.ResponseTypes.Sessions;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers.Sessions.RegisterBet
{
    public class RegisterBetHandler : BaseRequestHandler<RegisterBetRequest, bool>
    {
        public RegisterBetHandler(MonkifyDbContext context, IMessaging messaging, IHubContext<ActiveSessionsHub> activeSessionsHub, GeneralSettings settings) : base(context, messaging)
        {
            _activeSessionsHub = activeSessionsHub;
            _settings = settings;
        }

        private readonly IHubContext<ActiveSessionsHub> _activeSessionsHub;
        private readonly GeneralSettings _settings;

        private Session _session;
        private Bet _bet;

        public override async Task<bool> HandleRequest(RegisterBetRequest request, CancellationToken cancellationToken)
        {
            _bet = request.ToBet();

            await ValidateSession(request);
            await RegisterBet();
            await SendBet();

            return true;
        }

        private async Task ValidateSession(RegisterBetRequest request)
        {
            _session = await Context.Sessions.FirstOrDefaultAsync(x => x.Id == request.SessionId && x.Active && !x.EndDate.HasValue);

            if (_session is null)
                Messaging.ReturnValidationFailureMessage("The requested session was not found or is not active.");
        }

        private async Task RegisterBet()
        {
            await Context.SessionBets.AddAsync(_bet);
            var affectedRows = await Context.SaveChangesAsync();

            if (affectedRows <= 0)
            {
                Messaging.ReturnValidationFailureMessage("The system could not register this bet at the moment, please try again later.");
                Log.Error("An error occurred while trying to register a bet for the user {0} under the session {1}", _bet.UserId, _bet.SessionId);
            }
        }

        private async Task SendBet()
        {
            string sessionStatusEndpoint = string.Format(_settings.Sessions.SessionBetsEndpoint, _bet.SessionId.ToString());

            var sessionJson = JsonConvert.SerializeObject(new BetCreated("UserDefault", _bet.BetAmount, _bet.BetChoice));
            await _activeSessionsHub.Clients.All.SendAsync(sessionStatusEndpoint, sessionJson);
        }
    }
}
