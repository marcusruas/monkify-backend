using MassTransit;
using MassTransit.Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monkify.Common.Models;
using Monkify.Domain.Configs.Entities;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Abstractions
{
    public abstract class BaseWorker : BackgroundService
    {
        public BaseWorker(IServiceProvider services, IConfiguration configuration)
        {
            Services = services;
            Configuration = configuration;

            _workerName = GetType().Name;
        }

        protected readonly IServiceProvider Services;
        protected readonly IConfiguration Configuration;

        private string _workerName;

        protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Log.Information("Worker {0} started at {1}", _workerName, DateTime.UtcNow);
                    await ExecuteProcess(stoppingToken);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An exception occured on the worker {0} at {1}", _workerName, DateTime.UtcNow);
                }
                finally
                {
                    Log.Information("Worker {0} ended at {1}", _workerName, DateTime.UtcNow);
                }

                await Task.Delay(1000, stoppingToken);
            }
        }

        protected abstract Task ExecuteProcess(CancellationToken cancellationToken);

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

        protected void UseQueue(IModel channel, string queueName, bool durable = false)
            => channel.QueueDeclare(queue: queueName, durable: durable, exclusive: false, autoDelete: false, arguments: null);

        protected void PublishMessage(IModel channel, string queueName, string body, string exchange = "")
        {
            var bodyInBytes = Encoding.UTF8.GetBytes(body);
            channel.BasicPublish(exchange: exchange, routingKey: queueName, basicProperties: null, body: bodyInBytes);
        }
    }
}
