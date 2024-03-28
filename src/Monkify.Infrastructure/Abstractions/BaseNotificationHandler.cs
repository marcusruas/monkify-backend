using MediatR;
using Microsoft.Extensions.Configuration;
using Monkify.Common.Exceptions;
using Monkify.Common.Messaging;
using Monkify.Common.Models;
using Monkify.Infrastructure.Context;
using Newtonsoft.Json;
using RabbitMQ.Client;
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

        protected void ConnectToQueueChannel(string channelConnectionName, Action<IModel> channelOperations)
        {
            var channelConfiguration = Configuration.GetSection($"Channels:{channelConnectionName}").Get<ChannelConfiguration>();

            var factory = new ConnectionFactory() { HostName = channelConfiguration.Hostname, UserName = channelConfiguration.Username, Password = channelConfiguration.Password };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channelOperations(channel);
            }
        }

        protected void CreateQueue(IModel channel, string queueName, bool durable = false)
            => channel.QueueDeclare(queue: queueName, durable: durable, exclusive: false, autoDelete: false, arguments: null);

        protected void PublishMessage(IModel channel, string queueName, string body, string exchange = "")
        {
            var bodyInBytes = Encoding.UTF8.GetBytes(body);
            channel.BasicPublish(exchange: exchange, routingKey: queueName, basicProperties: null, body: bodyInBytes);
        }
    }
}
