using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using MediatR;
using Microsoft.Azure.Amqp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Monkify.Common.Messaging;
using Monkify.Domain.Monkey.Events;
using Monkify.Domain.Monkey.ValueObjects;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Context;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

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

            var bets = await Context.SessionBets.Where(x => x.SessionId == notification.SessionId).ToListAsync();
            bool sessionCanStart = bets.DistinctBy(x => x.UserId).Count() >= notification.MinimumNumberOfPlayers;

            ConnectToQueueChannel("Monkify", channel =>
            {
                CreateQueue(channel, notification.SessionId.ToString());

                SessionStatus status;

                if (!sessionCanStart)
                    status = new SessionStatus("There was not enough players to start the session. The session has ended.");
                else
                    status = new SessionStatus(QueueStatus.Started);

                PublishMessage(channel, notification.SessionId.ToString(), JsonConvert.SerializeObject(status));

                if (!sessionCanStart)
                    return;

                for (int i = 1; i < 10000; i++)
                {
                    PublishMessage(channel, notification.SessionId.ToString(), i.ToString());
                }

                var endSession = new SessionStatus(QueueStatus.Ended);
                PublishMessage(channel, notification.SessionId.ToString(), JsonConvert.SerializeObject(endSession));
            });
        }

        private async Task WaitForBets()
        {
            var intervalInSeconds = Configuration.GetSection("WaitPeriodForBets").Get<int>();
            await Task.Delay(intervalInSeconds * 1000);
        }
    }
}
