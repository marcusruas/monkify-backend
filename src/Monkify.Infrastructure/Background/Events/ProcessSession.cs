﻿using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Sessions;
using Newtonsoft.Json;
using Serilog;
using System.Collections.ObjectModel;
using System.Diagnostics;
using static Monkify.Domain.Sessions.ValueObjects.SessionStatus;

namespace Monkify.Infrastructure.Handlers.Sessions.Events
{
    public class ProcessSession : BaseNotificationHandler<SessionForProcessing>
    {
        public ProcessSession(MonkifyDbContext context, IMediator mediator, IHubContext<ActiveSessionsHub> hub, ISessionService sessionService, GeneralSettings settings)
        {
            _context = context;
            _mediator = mediator;
            _activeSessions = hub;
            _sessionService = sessionService;
            _sessionSettings = settings.Sessions;
        }

        private readonly MonkifyDbContext _context;
        private readonly IMediator _mediator;
        private readonly IHubContext<ActiveSessionsHub> _activeSessions;
        private readonly ISessionService _sessionService;
        private readonly SessionSettings _sessionSettings;

        private Session _session;
        private MonkifyTyper _monkey;

        public override async Task HandleRequest(SessionForProcessing notification, CancellationToken cancellationToken)
        {
            _session = notification.Session;

            bool sessionHasEnoughPlayers = false;

            while (!sessionHasEnoughPlayers && (_session.CreatedDate - DateTime.UtcNow).TotalMinutes <= _sessionSettings.WaitPeriodForBets)
            {
                _session.Bets = await _context.SessionBets.Where(x => x.SessionId == _session.Id).ToListAsync();
                sessionHasEnoughPlayers = _session.Bets.DistinctBy(x => x.Wallet)?.Count() >= _session.Parameters.MinimumNumberOfPlayers;

                if (!sessionHasEnoughPlayers)
                    await Task.Delay(5000, cancellationToken);
            }

            if (!sessionHasEnoughPlayers)
            {
                await _sessionService.UpdateSessionStatus(_session, NotEnoughPlayersToStart);
                await _sessionService.UpdateSessionStatus(_session, NeedsRefund);

                await Task.Delay(_sessionSettings.DelayBetweenSessions * 1000, cancellationToken);
                return;
            }

            _monkey = new MonkifyTyper(_session.Parameters.SessionCharacterType, _session.Bets);

            await _sessionService.UpdateSessionStatus(_session, Started);
            await SendTerminalCharacters();
            await _sessionService.UpdateSessionStatus(_session, Ended, _monkey);
            await DeclareWinners();

            await Task.Delay(_sessionSettings.DelayBetweenSessions * 1000, cancellationToken);
        }

        private async Task SendTerminalCharacters()
        {
            string terminalEndpoint = string.Format(_sessionSettings.SessionTerminalEndpoint, _session.Id.ToString());
            List<char> batch = new(_sessionSettings.TerminalBatchLimit);

            while (!_monkey.HasWinners)
            {
                batch.Add(_monkey.GenerateNextCharacter());
                if (batch.Count >= _sessionSettings.TerminalBatchLimit)
                    await SendBatch(terminalEndpoint, batch);
            }

            if (batch.Count > 0)
                await SendBatch(terminalEndpoint, batch);
        }

        private async Task SendBatch(string endpoint, List<char> batch)
        {
            await Task.Delay(_sessionSettings.DelayBetweenTerminalBatches);
            await _activeSessions.Clients.All.SendAsync(endpoint, batch);
            batch.Clear();
        }

        private async Task DeclareWinners()
        {
            if (!_monkey.HasWinners)
                return;

            _context.SessionBets.UpdateRange(_session.Bets);
            await _context.SaveChangesAsync();
            await _mediator.Publish(new RewardWinnersEvent(_session));
        }
    }
}
