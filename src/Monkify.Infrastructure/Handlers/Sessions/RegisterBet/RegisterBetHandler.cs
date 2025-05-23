﻿using Microsoft.AspNetCore.SignalR;
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
            _bet = new(request.SessionId, request.Body.Seed, request.Body.PaymentSignature, request.Body.Wallet, request.Body.Choice, request.Body.Amount.Value);

            await ValidateBetSignature();
            await ValidateBetParameters(request);
            await RegisterBet();
            await SendBet();

            return new BetDto(_bet);
        }

        private async Task ValidateBetSignature()
        {
            var signatureHasBeenUsed = await Context.SessionBets.AnyAsync(x => x.PaymentSignature == _bet.PaymentSignature);

            if (signatureHasBeenUsed)
                Messaging.ReturnValidationFailureMessage(ErrorMessages.PaymentSignatureHasBeenUsed);

            var paymentResult = await _solanaService.ValidateBetPayment(_bet);

            if (!paymentResult.IsValid)
                Messaging.ReturnValidationFailureMessage(paymentResult.ErrorMessage);
        }

        private async Task ValidateBetParameters(RegisterBetRequest request)
        {
            var session = await Context.Sessions
                .Include(x => x.Parameters)
                .ThenInclude(x => x.PresetChoices)
                .FirstOrDefaultAsync(x => x.Id == request.SessionId && Session.SessionAcceptingBets.Contains(x.Status));

            if (session is null)
                await RefundInvalidBet(ErrorMessages.SessionNotValidForBets);

            var betValidationResult = BetDomainService.ChoiceIsValidForSession(_bet, session);

            if (betValidationResult != BetValidationResult.Valid)
                await RefundInvalidBet(betValidationResult.StringValueOf());
        }

        private async Task RegisterBet()
        {
            await Context.SessionBets.AddAsync(_bet);
            var affectedRows = await Context.SaveChangesAsync();

            if (affectedRows <= 0)
            {
                Log.Error("An error occurred while trying to register a bet for the wallet {0} under the session {1}", _bet.Wallet, _bet.SessionId);
                await RefundInvalidBet(ErrorMessages.FailedToRegisterBet);
            }
        }

        private async Task SendBet()
        {
            string sessionBetsEndpoint = string.Format(_settings.Sessions.SessionBetsEndpoint, _bet.SessionId.ToString());

            var sessionJson = new BetCreatedEvent(_bet.Wallet, _bet.PaymentSignature, _bet.Amount, _bet.Choice).AsJson();
            await _recentBetsHub.Clients.All.SendAsync(sessionBetsEndpoint, sessionJson);
        }

        private async Task RefundInvalidBet(string errorMessage)
        {
            var errorMessageWithAdvice = string.Concat(errorMessage, " ", ErrorMessages.RefundWarning);

            var refundAmount = BetDomainService.CalculateRefundForInvalidBet(_settings.Token, _bet);
            await _solanaService.RefundTokens(_bet.Wallet, refundAmount);
            Messaging.ReturnValidationFailureMessage(errorMessageWithAdvice);
        }
    }
}