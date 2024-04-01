using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Monkify.Domain.Monkey.Events;
using Monkify.Domain.Monkey.Services;
using Monkify.Domain.Monkey.ValueObjects;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Context;
using Newtonsoft.Json;
using static Monkify.Infrastructure.Endpoints.QueuesEndpoints;

namespace Monkify.Infrastructure.Handlers.Sessions.Events
{
    public class ProcessSession : BaseNotificationHandler<SessionCreated>
    {
        public ProcessSession(MonkifyDbContext context, IConfiguration configuration) : base(context, configuration)
        {
        }

        public override async Task HandleRequest(SessionCreated notification, CancellationToken cancellationToken)
        {
            await WaitForBets();

            var sessionId = notification.SessionId.ToString();
            var bets = await Context.SessionBets.Where(x => x.SessionId == notification.SessionId).ToListAsync();
            bool sessionCanStart = bets.DistinctBy(x => x.UserId).Count() >= notification.MinimumNumberOfPlayers;

            ConnectToQueueChannel("Monkify", channel =>
            {
                UseQueue(channel, string.Format(SESSION_STATUS_ENDPOINT, sessionId));

                SessionStatus status;

                if (!sessionCanStart)
                    status = new SessionStatus("There was not enough players to start the session. The session has ended.");
                else
                    status = new SessionStatus(QueueStatus.Started);

                PublishMessage(channel, string.Format(SESSION_STATUS_ENDPOINT, sessionId), JsonConvert.SerializeObject(status));

                if (!sessionCanStart)
                    return;

                UseQueue(channel, string.Format(SESSION_TERMINAL_ENDPOINT, sessionId));

                var monkey = new MonkifyTyper(notification.CharacterType, bets);

                while(!monkey.HasWinners)
                {
                    var character = monkey.GenerateNextCharacter();
                    PublishMessage(channel, string.Format(SESSION_TERMINAL_ENDPOINT, sessionId), character.ToString());
                }

                UseQueue(channel, string.Format(SESSION_STATUS_ENDPOINT, sessionId));

                var endSession = new SessionStatus(QueueStatus.Ended);
                endSession.EndResult = new SessionEndResult(monkey.Winners.Count(), monkey.FirstChoiceTyped);
                PublishMessage(channel, string.Format(SESSION_STATUS_ENDPOINT, sessionId), JsonConvert.SerializeObject(endSession));
            });
        }
         
        private async Task WaitForBets()
        {
            var intervalInSeconds = Configuration.GetSection("WaitPeriodForBets").Get<int>();
            await Task.Delay(intervalInSeconds * 1000);
        }
    }
}
