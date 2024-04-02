using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Monkey.Entities;
using Monkify.Domain.Monkey.Events;
using Monkify.Domain.Monkey.Services;
using Monkify.Domain.Monkey.ValueObjects;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Handlers.Sessions.Hubs;
using Newtonsoft.Json;

namespace Monkify.Infrastructure.Handlers.Sessions.Events
{
    public class ProcessSession : BaseNotificationHandler<SessionCreated>
    {
        public ProcessSession(MonkifyDbContext context, SessionSettings sessionSettings, IHubContext<ActiveSessionsHub> hub)
        {
            _context = context;
            _activeSessions = hub;
            _sessionSettings = sessionSettings;
        }

        private readonly MonkifyDbContext _context;
        private readonly IHubContext<ActiveSessionsHub> _activeSessions;
        private readonly SessionSettings _sessionSettings;

        private string _sessionId;
        private List<Bet> _betsForSession;
        private bool _sessionHasEnoughPlayers;

        private MonkifyTyper _monkey;

        public override async Task HandleRequest(SessionCreated notification, CancellationToken cancellationToken)
        {
            await Task.Delay(_sessionSettings.WaitPeriodForBets * 1000);

            _sessionId = notification.SessionId.ToString();
            _betsForSession = await _context.SessionBets.Where(x => x.SessionId == notification.SessionId).ToListAsync();
            _sessionHasEnoughPlayers = _betsForSession.DistinctBy(x => x.UserId).Count() >= notification.MinimumNumberOfPlayers;

            await SendSessionInitialStatus();

            if (!_sessionHasEnoughPlayers)
                return;

            await SendTerminalCharacters(notification);

            await SendSessionEndStatus();
        }

        private async Task SendSessionInitialStatus()
        {
            SessionStatus status;

            if (!_sessionHasEnoughPlayers)
                status = new SessionStatus("There was not enough players to start the session. The session has ended.");
            else
                status = new SessionStatus(QueueStatus.Started);

            await SendSessionStatus(status);
        }

        private async Task SendTerminalCharacters(SessionCreated notification)
        {
            _monkey = new MonkifyTyper(notification.CharacterType, _betsForSession);
            string terminalEndpoint = string.Format(_sessionSettings.SessionTerminalEndpoint, _sessionId);
            List<char> batch = new();

            while (!_monkey.HasWinners)
            {
                var character = _monkey.GenerateNextCharacter();
                batch.Add(character);

                if (batch.Count >= _sessionSettings.TerminalBatchLimit)
                {
                    await Task.Delay(_sessionSettings.DelayBetweenTerminalBatches);
                    await _activeSessions.Clients.All.SendAsync(terminalEndpoint, batch);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await Task.Delay(_sessionSettings.DelayBetweenTerminalBatches);
                await _activeSessions.Clients.All.SendAsync(terminalEndpoint, batch);
            }
        }

        private async Task SendSessionEndStatus()
        {
            var status = new SessionStatus(QueueStatus.Ended);
            status.EndResult = new SessionEndResult(_monkey.Winners.Count(), _monkey.FirstChoiceTyped);

            await SendSessionStatus(status);
        }

        private async Task SendSessionStatus(SessionStatus status)
        {
            string sessionStatusEndpoint = string.Format(_sessionSettings.SessionStatusEndpoint, _sessionId);

            var sessionJson = JsonConvert.SerializeObject(status);
            await _activeSessions.Clients.All.SendAsync(sessionStatusEndpoint, sessionJson);
        }
    }
}
