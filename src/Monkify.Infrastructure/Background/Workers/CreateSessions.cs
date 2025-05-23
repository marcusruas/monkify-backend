﻿using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
using System.Threading;

namespace Monkify.Infrastructure.Background.Workers
{
    public class CreateSessions : BaseWorker
    {
        public CreateSessions(IServiceProvider services) : base(services) { }

        public override async Task ExecuteProcess(CancellationToken cancellationToken)
        {
            using (var upperScope = Services.CreateScope())
            {
                var settings = upperScope.GetService<GeneralSettings>();
                var context = upperScope.GetService<MonkifyDbContext>();

                var activeParameters = await context.SessionParameters
                    .Include(x => x.PresetChoices)
                    .Where(x => x.Active && !x.Sessions.Any(y => Session.SessionInProgressStatus.Contains(y.Status)))
                    .ToListAsync(cancellationToken);

                foreach (var parameters in activeParameters)
                {
                    _ = Task.Run(() => CreateNewSession(parameters, settings, cancellationToken), cancellationToken);
                }

                await Task.Delay(settings.Workers.CreateSessionsInterval * 1000, cancellationToken);
                return;
            }
        }

        private async Task CreateNewSession(SessionParameters parameters, GeneralSettings settings, CancellationToken cancellationToken)
        {
            using var innerScope = Services.CreateScope();

            var innerContext = innerScope.GetService<MonkifyDbContext>();
            var mediator = innerScope.GetService<IMediator>();
            var openSessionsHub = innerScope.GetService<IHubContext<ActiveSessionsHub>>();

            var session = new Session(parameters.Id);

            await innerContext.Sessions.AddAsync(session, cancellationToken);
            await innerContext.SaveChangesAsync(cancellationToken);

            session.Parameters = parameters;

            var sessionCreatedEvent = new SessionCreatedEvent(session.Id, parameters);
            await openSessionsHub.Clients.All.SendAsync(settings.Sessions.ActiveSessionsEndpoint, sessionCreatedEvent.AsJson(), cancellationToken);

            await mediator.Publish(new SessionStartEvent(session), cancellationToken);
        }
    }
}
