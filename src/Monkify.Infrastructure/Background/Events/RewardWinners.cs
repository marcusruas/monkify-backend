using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Sessions;
using Monkify.Infrastructure.Services.Solana;
using Newtonsoft.Json;
using Serilog;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Core.Http;
using Solnet.Wallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Background.Events
{
    public class RewardWinners : BaseNotificationHandler<RewardWinnersEvent>
    {
        public RewardWinners(MonkifyDbContext context, ISolanaService client, ISessionService sessionService, GeneralSettings settings)
        {
            _context = context;
            _solanaService = client;
            _sessionService = sessionService;
            _settings = settings;
        }

        private readonly MonkifyDbContext _context;
        private readonly ISolanaService _solanaService;
        private readonly ISessionService _sessionService;
        private readonly GeneralSettings _settings;

        private BetValidator _betValidator;

        public override async Task HandleRequest(RewardWinnersEvent notification, CancellationToken cancellationToken)
        {
            _betValidator = new(notification.Session, _settings.Token);

            await _sessionService.UpdateSessionStatus(notification.Session, SessionStatus.RewardForWinnersInProgress);

            try
            {
                foreach (var winner in _betValidator.Winners)
                {
                    var rewardResult = _betValidator.CalculateRewardForBet(winner);
                    await _solanaService.TransferTokens(winner.Id, winner.Wallet, rewardResult);
                }
            }
            catch
            {
                await _sessionService.UpdateSessionStatus(notification.Session, SessionStatus.NeedsRefund);
                return;
            }

            await _sessionService.UpdateSessionStatus(notification.Session, SessionStatus.RewardForWinnersCompleted);
        }
    }
}
