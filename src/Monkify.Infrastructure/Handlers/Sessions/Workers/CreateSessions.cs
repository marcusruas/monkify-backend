using MassTransit;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monkify.Common.Extensions;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Monkey.Entities;
using Monkify.Domain.Monkey.Events;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Handlers.Sessions.Hubs;
using Newtonsoft.Json;
using Serilog;

namespace Monkify.Infrastructure.Handlers.Sessions.Workers
{
    public class CreateSessions : BaseWorker
    {
        public CreateSessions(IServiceProvider services) : base(services) { }

        protected override async Task ExecuteProcess(CancellationToken cancellationToken)
        {
            using (var scope = Services.CreateScope())
            {
                var sessionConfigs = scope.GetService<SessionSettings>();
                var context = scope.GetService<MonkifyDbContext>();
                var mediator = scope.GetService<IMediator>();
                var hub = scope.GetService<IHubContext<OpenSessionsHub>>();

                var activeParameters = await context.SessionParameters.Where(x => x.Active).ToListAsync();

                foreach(var parameters in activeParameters)
                {
                    var sessionIsOpen = await context.Sessions.AnyAsync(x => x.ParametersId == parameters.Id && x.Active);

                    if (sessionIsOpen)
                        return;

                    var newSession = new Session(parameters.Id);

                    if (!await SessionCreated(context, newSession))
                        return;

                    var sessionCreatedEvent = new SessionCreated(newSession.Id, parameters);
                    
                    var sessionJson = JsonConvert.SerializeObject(sessionCreatedEvent);
                    await hub.Clients.All.SendAsync(sessionConfigs.ActiveSessionsEndpoint, sessionJson);

                    await mediator.Publish(sessionCreatedEvent, cancellationToken);
                }
            }
        }

        private async Task<bool> SessionCreated(MonkifyDbContext context, Session session)
        {
            await context.AddAsync(session);
            var affectedRows = await context.SaveChangesAsync();
            context.Entry(session).State = EntityState.Detached;

            bool operationSucceeded = affectedRows > 0;

            if (!operationSucceeded)
                Log.Error("Failed to open a new session for {0}", nameof(CreateSessions));

            return operationSucceeded;
        }
    }
}
