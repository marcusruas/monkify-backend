using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Monkify.Common.Extensions;
using Monkify.Common.Messaging;
using Monkify.Common.Resources;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Configs.ValueObjects;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.ResponseTypes.Sessions;
using Monkify.Infrastructure.Services.Solana;
using Serilog;

namespace Monkify.Infrastructure.Handlers.Sessions.RegisterBet
{
    public class RegisterBetHandler : BaseRequestHandler<RegisterBetRequest, BetDto>
    {
        public RegisterBetHandler(
            MonkifyDbContext context, 
            IMessaging messaging, 
            IHubContext<RecentBetsHub> activeSessionsHub, GeneralSettings settings,
            ISolanaService solanaService
        ) : base(context, messaging)
        {
            _recentBetsHub = activeSessionsHub;
            _settings = settings;
            _solanaService = solanaService;
        }

        private readonly IHubContext<RecentBetsHub> _recentBetsHub;
        private readonly GeneralSettings _settings;
        private readonly ISolanaService _solanaService;

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
            var session = await Context.Sessions
                .Include(x => x.Parameters)
                .ThenInclude(x => x.PresetChoices)
                .FirstOrDefaultAsync(x => x.Id == request.SessionId && Session.SessionAcceptingBets.Contains(x.Status));

            if (session is null)
                Messaging.ReturnValidationFailureMessage(ErrorMessages.SessionNotValidForBets);

            _bet = new(request.SessionId, request.Body.Seed, request.Body.PaymentSignature, request.Body.Wallet, request.Body.Choice, request.Body.Amount.Value);

            var betValidationResult = BetDomainService.ChoiceIsValidForSession(_bet, session);

            if (betValidationResult != BetValidationResult.Valid)
                Messaging.ReturnValidationFailureMessage(betValidationResult.StringValueOf());

            var signatureHasBeenUsed = await Context.SessionBets.AnyAsync(x => x.PaymentSignature == request.Body.PaymentSignature);

            if (signatureHasBeenUsed)
                Messaging.ReturnValidationFailureMessage(ErrorMessages.PaymentSignatureHasBeenUsed);

            var paymentResult = await _solanaService.ValidateBetPayment(_bet);

            if (!paymentResult.IsValid)
                Messaging.ReturnValidationFailureMessage(paymentResult.ErrorMessage);
        }

        private async Task RegisterBet()
        {
            await Context.SessionBets.AddAsync(_bet);
            var affectedRows = await Context.SaveChangesAsync();

            if (affectedRows <= 0)
            {
                Messaging.ReturnValidationFailureMessage(ErrorMessages.FailedToRegisterBet);
                Log.Error("An error occurred while trying to register a bet for the wallet {0} under the session {1}", _bet.Wallet, _bet.SessionId);
            }
        }

        private async Task SendBet()
        {
            string sessionBetsEndpoint = string.Format(_settings.Sessions.SessionBetsEndpoint, _bet.SessionId.ToString());

            var sessionJson = new BetCreated(_bet.Wallet, _bet.PaymentSignature, _bet.Amount, _bet.Choice).AsJson();
            await _recentBetsHub.Clients.All.SendAsync(sessionBetsEndpoint, sessionJson);
        }
    }
}