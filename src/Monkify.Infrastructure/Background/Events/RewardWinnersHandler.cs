using Microsoft.AspNetCore.SignalR;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Background.Events
{
    public class RewardWinnersHandler : BaseNotificationHandler<RewardWinnersEvent>
    {
        public RewardWinnersHandler(MonkifyDbContext context, GeneralSettings settings)
        {
            _context = context;
            _sessionSettings = settings.Sessions;
        }

        private readonly MonkifyDbContext _context;
        private readonly SessionSettings _sessionSettings;

        public override Task HandleRequest(RewardWinnersEvent notification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
