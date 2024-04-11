using Microsoft.Extensions.Hosting;
using Serilog;

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
            await Task.Delay(1000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Log.Information("Worker {0}: started", _workerName);
                    await ExecuteProcess(stoppingToken);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Worker{0}: An exception occurred", _workerName);
                }
                finally
                {
                    Log.Information("Worker {0}: ended", _workerName);
                }
            }
        }

        protected abstract Task ExecuteProcess(CancellationToken cancellationToken);
    }
}
