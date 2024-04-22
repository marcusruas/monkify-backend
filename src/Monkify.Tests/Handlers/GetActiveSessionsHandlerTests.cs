using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Handlers.Sessions.GetActiveSessions;
using Monkify.Tests.Shared;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.Handlers
{
    public class GetActiveSessionsHandlerTests : UnitTestsClass
    {
        [Fact]
        public async Task GetActiveSessions_WithActiveSessions_ShouldReturnSuccess()
        {
            var presetChoices = new List<PresetChoice>() { new PresetChoice(Faker.Random.String2(4)), new PresetChoice(Faker.Random.String2(4)) };
            var session = new Session();
            session.Status = SessionStatus.WaitingBets;
            session.Parameters = new SessionParameters() { AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter, PresetChoices = presetChoices };
            session.Bets = new List<Bet>() { new(session.Id, Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2) };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var request = new GetActiveSessionsRequest();
                var handler = new GetActiveSessionsHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.ShouldNotBeEmpty();
            }
        }

        [Fact]
        public async Task GetActiveSessions_WithoutActiveSessions_ShouldReturnSuccess()
        {
            using (var context = new MonkifyDbContext(ContextOptions))
            {
                var request = new GetActiveSessionsRequest();
                var handler = new GetActiveSessionsHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.ShouldBeEmpty();
            }
        }
    }
}
