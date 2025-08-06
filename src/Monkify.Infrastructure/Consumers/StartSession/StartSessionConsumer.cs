using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Infrastructure.Abstractions.KafkaHandlers;
using Monkify.Infrastructure.Context;

namespace Monkify.Infrastructure.Consumers.StartSession
{
    public class StartSessionConsumer : BaseConsumer<StartSessionEvent>
    {
        public StartSessionConsumer(IServiceProvider services, IOptionsMonitor<ConsumerOptions> options) : base(options)
        {
            _services = services;
        }

        private IServiceProvider _services;

        protected override async Task ConsumeAsync(StartSessionEvent message, CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonkifyDbContext>();

            var session = new Session(message.SessionParameters.Id);

            await context.Sessions.AddAsync(session, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            session.Parameters = message.SessionParameters;

            //Send to WS
        }
    }
}
