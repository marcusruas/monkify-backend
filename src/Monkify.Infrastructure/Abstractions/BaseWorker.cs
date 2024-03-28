using MassTransit;
using MassTransit.Mediator;
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
        public BaseWorker(IServiceProvider services, IConfiguration configuration)
        {
            Services = services;
            Configuration = configuration;
            _workerName = GetType().Name;
            _workerInterval = GetWorkerInterval();
        }

        protected readonly IServiceProvider Services;

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

        private int GetWorkerInterval()
        {
            var workerIntervals = Configuration.GetSection("WorkerSettings:Intervals");
            return int.Parse(workerIntervals[_workerName]);
        }
    }
}
