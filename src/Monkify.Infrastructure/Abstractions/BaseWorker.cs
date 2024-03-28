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
        public BaseWorker(IServiceProvider services)
        {
            Services = services;
            _workerName = GetType().Name;
        }

        protected readonly IServiceProvider Services;

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
    }
}
