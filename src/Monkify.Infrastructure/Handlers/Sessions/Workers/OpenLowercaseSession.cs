using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monkify.Common.Extensions;
using Monkify.Domain.Monkey.Entities;
using Monkify.Domain.Monkey.ValueObjects;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Context;
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

                await CreateSession(context, newSession);
            }
        }

        private async Task CreateSession(MonkifyDbContext context, Session session)
        {
            await context.AddAsync(session);
            var affectedRows = await context.SaveChangesAsync();

            if (affectedRows <= 0)
                Log.Error("Failed to open a new session for {0}", nameof(OpenLowercaseSession));
        }
    }
}
