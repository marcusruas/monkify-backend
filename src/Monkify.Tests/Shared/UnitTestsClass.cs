using Bogus;
using Microsoft.EntityFrameworkCore;
using Monkify.Common.Exceptions;
using Monkify.Common.Messaging;
using Monkify.Infrastructure.Context;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.Shared
{
    public abstract class UnitTestsClass
    {
        public UnitTestsClass()
        {
            CancellationToken = new CancellationToken();
            Faker = new Faker();
            Messaging = new Messaging();
            ContextOptions = new DbContextOptionsBuilder<MonkifyDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        }

        protected readonly IMessaging Messaging;
        protected readonly DbContextOptions<MonkifyDbContext> ContextOptions;
        protected readonly Faker Faker;
        protected CancellationToken CancellationToken;

        protected async Task ShouldReturnValidationFailure(Task action, string expectedErrorMessage)
        {
            await Should.ThrowAsync<ValidationFailureException>(action);

            Messaging.HasValidationFailures().ShouldBeTrue();
            Messaging.Messages.Any(x => x.Type == MessageType.ValidationFailure && x.Value == expectedErrorMessage).ShouldBeTrue();
        }

        protected async Task ShouldReturnError(Task action, string expectedErrorMessage)
        {
            await Should.ThrowAsync<InternalErrorException>(action);

            Messaging.HasErrors().ShouldBeTrue();
            Messaging.Messages.Any(x => x.Type == MessageType.Error && x.Value == expectedErrorMessage).ShouldBeTrue();
        }
    }
}
