using MediatR;
using Microsoft.Extensions.Configuration;
using Monkify.Common.Exceptions;
using Monkify.Common.Messaging;
using Monkify.Infrastructure.Context;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Abstractions
{
    public abstract class BaseNotificationHandler<TNotification> : INotificationHandler<TNotification> where TNotification : INotification
    {
        public BaseNotificationHandler(MonkifyDbContext context, IMessaging messaging, IMediator mediator, IConfiguration configuration)
        {
            Context = context;
            Messaging = messaging;
            Mediator = mediator;
            Configuration = configuration;
        }

        protected readonly MonkifyDbContext Context;
        protected readonly IMessaging Messaging;
        protected readonly IMediator Mediator;
        protected readonly IConfiguration Configuration;

        public async Task Handle(TNotification notification, CancellationToken cancellationToken)
        {
            try
            {
                await HandleRequest(notification, cancellationToken);
            }
            catch (Exception ex)
            {
                var requestJson = JsonConvert.SerializeObject(notification);
                Log.Error(ex, "The request to the event handler {handler} failed. Request: {request}", GetType().Name, requestJson);
            }
        }

        public abstract Task HandleRequest(TNotification notification, CancellationToken cancellationToken);

        protected string? GetQueueConnectionString(string queueName)
        {
            var queuesSection = Configuration.GetSection("Queues");
            return queuesSection[queueName];
        }
    }
}
