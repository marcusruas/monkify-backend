using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Monkify.Common.Extensions;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.Services;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Background.Hubs;
using Serilog;
using Monkify.Domain.Configs.ValueObjects;
using Monkify.Infrastructure.Background.Events.StartSession;
using Monkify.Infrastructure.Services.Sessions;
using Monkify.Domain.Sessions.ValueObjects;

namespace Monkify.Infrastructure.Background.Events.BetPlaced
{
    public class BetPlacedHandler : BaseNotificationHandler<BetPlacedEvent>
    {
        public BetPlacedHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private readonly IServiceProvider _serviceProvider;

        public override async Task HandleRequest(BetPlacedEvent notification, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var tracker = scope.ServiceProvider.GetRequiredService<SessionBetsTracker>();
            var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
            await tracker.AddBetAsync(notification.Bet, cancellationToken);

            await SendBetToWebSocket(scope, notification.Bet);

            if (!await SessionHasEnoughPlayers(scope, notification.Bet, cancellationToken))
            {
                return;
            }

            bool sessionStarted = await sessionService.TryStartSession(notification.Bet.Session);

            // Only publish the StartSessionEvent if this call successfully started the session
            if (sessionStarted)
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Publish(new StartSessionEvent(notification.Bet.Session));
            }
        }

        private async Task SendBetToWebSocket(IServiceScope scope, Bet bet)
        {
            var settings = scope.ServiceProvider.GetRequiredService<GeneralSettings>();
            var recentBetsHub = scope.ServiceProvider.GetRequiredService<IHubContext<RecentBetsHub>>();
            
            string sessionBetsEndpoint = string.Format(settings.Sessions.SessionBetsEndpoint, bet.SessionId.ToString());

            var sessionJson = new BetNotificationEvent(bet.Wallet, bet.PaymentSignature, bet.Amount, bet.Choice).AsJson();
            await recentBetsHub.Clients.All.SendAsync(sessionBetsEndpoint, sessionJson);
        }

        private async Task<bool> SessionHasEnoughPlayers(IServiceScope scope, Bet bet, CancellationToken cancellationToken)
        {
            var settings = scope.ServiceProvider.GetRequiredService<GeneralSettings>();
            var tracker = scope.ServiceProvider.GetRequiredService<SessionBetsTracker>();
            
            var elapsedTimeSinceCreation = (DateTime.UtcNow - bet.Session.CreatedDate).TotalSeconds;
            bool minimumTimeElapsed = elapsedTimeSinceCreation > settings.Sessions.MinimumWaitPeriodForBets;

            if (!minimumTimeElapsed)
                return false;

            return await tracker.SessionHasEnoughPlayersAsync(bet.Session.Id, bet.Session.Parameters.MinimumNumberOfPlayers, cancellationToken);
        }
    }
}
