using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        public BaseWorker(IServiceProvider services, IBus bus, IConfiguration configuration)
        {
            Services = services;
            BusControl = bus;
            Configuration = configuration;
            _workerName = GetType().Name;
            _workerInterval = GetWorkerInterval();
        }

        protected readonly IServiceProvider Services;

        private readonly IBus BusControl;
        private readonly IConfiguration Configuration;
        private string _workerName;
        private int _workerInterval;

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

                await Task.Delay(_workerInterval * 1000, stoppingToken);
            }
        }

        protected abstract Task ExecuteProcess(CancellationToken cancellationToken);

        protected string? GetQueueConnectionString(string queueName)
        {
            var queuesSection = Configuration.GetSection("Queues");
            return queuesSection[queueName];
        }

        protected async Task SendMessage<C, T>(T message, string queueConnectionString) where C : IConsumer<T> where T : class
        {
            if (string.IsNullOrWhiteSpace(queueConnectionString))
            {
                string error = "The requested connection string could not be found. This error occured on the Worker {0} at {1}";
                Log.Error(error, _workerName, DateTime.UtcNow);
                throw new ArgumentException(string.Format(error, _workerName, DateTime.UtcNow));
            }

            try
            {
                var endpointUri = new Uri($"{queueConnectionString}/{typeof(C).Name}");
                var endpoint = await BusControl.GetSendEndpoint(endpointUri);

                await endpoint.Send(message);
            }
            catch (Exception ex)
            {
                var serializedMessage = JsonConvert.SerializeObject(message);
                Log.Error(ex, "A message was failed to be sent to the consumer {0} at {1}. Message details: {2}", typeof(T).Name, DateTime.UtcNow, serializedMessage);
            }
        }

        private int GetWorkerInterval()
        {
            var workerIntervals = Configuration.GetSection("WorkerSettings:Intervals");
            return int.Parse(workerIntervals[_workerName]);
        }
    }
}
