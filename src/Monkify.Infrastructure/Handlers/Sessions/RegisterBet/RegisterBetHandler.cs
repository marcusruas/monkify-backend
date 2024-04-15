using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Monkify.Common.Extensions;
using Monkify.Common.Messaging;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Configs.ValueObjects;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.Services;
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
    public class RegisterBetHandler : BaseRequestHandler<RegisterBetRequest, BetDto>
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

        public override async Task<BetDto> HandleRequest(RegisterBetRequest request, CancellationToken cancellationToken)
        {
            await ValidateBet(request);
            await RegisterBet();
            await SendBet();

            return new BetDto(_bet);
        }

        private async Task ValidateBet(RegisterBetRequest request)
        {
            _session = await Context.Sessions
                .Include(x => x.Parameters)
                .ThenInclude(x => x.PresetChoices)
                .FirstOrDefaultAsync(x => x.Id == request.SessionId && x.Status == SessionStatus.WaitingBets);

            if (_session is null)
                Messaging.ReturnValidationFailureMessage("The requested session was not found or is not receiving bets at the current moment.");

            _bet = request.ToBet();

            var betValidationResult = BetValidator.ChoiceIsValidForSession(_bet, _session);

            if (betValidationResult != BetValidationResult.Valid)
                Messaging.ReturnValidationFailureMessage(betValidationResult.StringValueOf());
        }

        private async Task RegisterBet()
        {
            await Context.SessionBets.AddAsync(_bet);
            var affectedRows = await Context.SaveChangesAsync();

            if (affectedRows <= 0)
            {
                Messaging.ReturnValidationFailureMessage("The system could not register this bet at the moment, please try again later.");
                Log.Error("An error occurred while trying to register a bet for the wallet {0} under the session {1}", _bet.Wallet, _bet.SessionId);
            }
        }

        private async Task SendBet()
        {
            string sessionStatusEndpoint = string.Format(_settings.Sessions.SessionBetsEndpoint, _bet.SessionId.ToString());

            var sessionJson = new BetCreated(_bet.Wallet, _bet.Amount, _bet.Choice).AsJson();
            await _activeSessionsHub.Clients.All.SendAsync(sessionStatusEndpoint, sessionJson);
        }
    }
}
