using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monkify.Common.Extensions;
using Monkify.Domain.Monkey.Entities;
using Monkify.Domain.Monkey.Events;
using Monkify.Domain.Monkey.ValueObjects;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Handlers.Sessions.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers.Sessions.Workers
{
    public class OpenSessions : BaseWorker
    {
        public OpenSessions(IServiceProvider services) : base(services) { }

        protected override async Task ExecuteProcess(CancellationToken cancellationToken)
        {
            using (var scope = Services.CreateScope())
            {
                var context = scope.GetService<MonkifyDbContext>();
                var mediator = scope.GetService<IMediator>();

                var activeParameters = await context.SessionParameters.Where(x => x.Active).ToListAsync();

                foreach(var parameters in activeParameters)
                {
                    var sessionIsOpen = await context.Sessions.AnyAsync(x => x.ParametersId == parameters.Id && x.Active);

                    if (sessionIsOpen)
                        return;

                    var newSession = new Session(parameters.Id);

                    if (!await SessionCreated(context, newSession))
                        return;

                    var sessionCreatedEvent = new SessionCreated(newSession.Id, parameters.SessionCharacterType, parameters.MinimumNumberOfPlayers);
                    await mediator.Publish(sessionCreatedEvent, cancellationToken);
                }
            }
        }

        private async Task<bool> SessionCreated(MonkifyDbContext context, Session session)
        {
            await context.AddAsync(session);
            var affectedRows = await context.SaveChangesAsync();

            bool operationSucceeded = affectedRows > 0;

            if (!operationSucceeded)
                Log.Error("Failed to open a new session for {0}", nameof(OpenSessions));

            return operationSucceeded;
        }
    }
}
