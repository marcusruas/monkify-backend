﻿using MassTransit;
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
    public class OpenLowercaseSession : BaseWorker
    {
        public OpenLowercaseSession(IServiceProvider services, IBus bus, IConfiguration configuration) : base(services, bus, configuration) { }

        protected override async Task ExecuteProcess(CancellationToken cancellationToken)
        {
            using (var scope = Services.CreateScope())
            {
                var context = scope.GetService<MonkifyDbContext>();

                var sessionIsOpen = await context.Sessions.AnyAsync(x => x.SessionCharacterType == SessionCharacterType.LowerCaseLetter && x.Active);

                if (sessionIsOpen)
                    return;

                var newSession = new Session(SessionCharacterType.LowerCaseLetter);

                var operationSucceeded = await SessionCreated(context, newSession);

                if (!operationSucceeded)
                    return;

                var mediator = scope.GetService<IMediator>();
                var sessionCreatedEvent = new SessionCreated(newSession.Id, newSession.SessionCharacterType);
                await mediator.Publish(sessionCreatedEvent, cancellationToken);
            }
        }

        private async Task<bool> SessionCreated(MonkifyDbContext context, Session session)
        {
            await context.AddAsync(session);
            var affectedRows = await context.SaveChangesAsync();

            bool operationSucceeded = affectedRows > 0;

            if (!operationSucceeded)
                Log.Error("Failed to open a new session for {0}", nameof(OpenLowercaseSession));

            return operationSucceeded;
        }
    }
}
