using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monkify.Common.Extensions;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Context;
using Newtonsoft.Json;
using Serilog;

namespace Monkify.Infrastructure.Background.Workers
{
    public class CreateSessions : BaseWorker
    {
        public CreateSessions(IServiceProvider services) : base(services) { }

        protected override async Task ExecuteProcess(CancellationToken cancellationToken)
        {
            using (var scope = Services.CreateScope())
            {
                var sessionConfigs = scope.GetService<GeneralSettings>();
                var context = scope.GetService<MonkifyDbContext>();
                var mediator = scope.GetService<IMediator>();
                var openSessionsHub = scope.GetService<IHubContext<OpenSessionsHub>>();

                var activeParameters = await context.SessionParameters.Where(x => x.Active).ToListAsync();

                var parametersTasks = new List<Task>();

                foreach (var parameters in activeParameters)
                {
                    var sessionIsOpen = await context.Sessions.AnyAsync(x => x.ParametersId == parameters.Id && Session.SessionInProgressStatus.Contains(x.Status));

                    if (sessionIsOpen)
                        return;

                    parametersTasks.Add(Task.Run(async () =>
                    {
                        var session = await CreateSession(context, parameters.Id);
                        
                        var sessionCreatedEvent = new SessionCreated(session.Id, parameters);
                        var sessionJson = sessionCreatedEvent.AsJson();
                        await openSessionsHub.Clients.All.SendAsync(sessionConfigs.Sessions.ActiveSessionsEndpoint, sessionJson);

                        await mediator.Publish(sessionCreatedEvent, cancellationToken);
                    }, cancellationToken));
                }

                await Task.WhenAll(parametersTasks);
            }
        }

        private async Task<Session> CreateSession(MonkifyDbContext context, Guid parametersId)
        {
            var session = new Session(parametersId);
            await context.Sessions.AddAsync(session);

            await context.SaveChangesAsync();

            return session;
        }
    }
}
