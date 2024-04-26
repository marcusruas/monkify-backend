using Monkify.Common.Resources;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Handlers.Sessions.GetActiveSessionForParameter;
using Monkify.Tests.Shared;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.Handlers
{
    public class GetActiveSessionForParameterTests : UnitTestsClass
    {
        [Fact]
        public async Task GetSession_ShouldReturnData()
        {
            var session = new Session();
            session.Status = SessionStatus.WaitingBets;
            session.Parameters = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                SessionCharacterType = SessionCharacterType.LowerCaseLetter,
                Active = true,
            };
            session.Bets = new List<Bet>()
            {
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var request = new GetActiveSessionForParameterRequest(session.Parameters.Id);
                var handler = new GetActiveSessionForParameterHandler(context, Messaging);

                var result = await handler.Handle(request, CancellationToken);

                result.ShouldNotBeNull();
                result.SessionId.ShouldBe(session.Id);
                result.Bets.ShouldNotBeNull();
                result.Bets.ShouldNotBeEmpty();
            }
        }

        [Fact]
        public async Task GetSession_NonExistingParameter_ShouldReturnError()
        {
            using (var context = new MonkifyDbContext(ContextOptions))
            {
                var request = new GetActiveSessionForParameterRequest(Guid.NewGuid());
                var handler = new GetActiveSessionForParameterHandler(context, Messaging);

                await ShouldReturnValidationFailure(handler.Handle(request, CancellationToken), ErrorMessages.ParameterNotFound);
            }
        }

        [Fact]
        public async Task GetSession_InactiveParameter_ShouldReturnError()
        {
            var session = new Session();
            session.Status = SessionStatus.WaitingBets;
            session.Parameters = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                SessionCharacterType = SessionCharacterType.LowerCaseLetter,
                Active = false,
            };
            session.Bets = new List<Bet>()
            {
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var request = new GetActiveSessionForParameterRequest(session.Parameters.Id);
                var handler = new GetActiveSessionForParameterHandler(context, Messaging);

                await ShouldReturnValidationFailure(handler.Handle(request, CancellationToken), ErrorMessages.ParameterNotFound);
            }
        }

        [Fact]
        public async Task GetSession_NoSessionForParameter_ShouldReturnData()
        {
            var parameters = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                SessionCharacterType = SessionCharacterType.LowerCaseLetter,
                Active = true,
            };
            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(parameters);
                context.SaveChanges();

                var request = new GetActiveSessionForParameterRequest(parameters.Id);
                var handler = new GetActiveSessionForParameterHandler(context, Messaging);

                await ShouldReturnValidationFailure(handler.Handle(request, CancellationToken), ErrorMessages.ParameterHasNoActiveSessions);
            }
        }

        [Fact]
        public async Task GetSession_ParameterWithoutActiveSessions_ShouldReturnData()
        {
            var session = new Session();
            session.Status = SessionStatus.ErrorWhenProcessingRewards;
            session.Parameters = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                SessionCharacterType = SessionCharacterType.LowerCaseLetter,
                Active = true,
            };
            session.Bets = new List<Bet>()
            {
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new(session.Id, Faker.Random.String2(40), Faker.Random.String2(80), Faker.Random.String2(44), Faker.Random.String2(4), 2),
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var request = new GetActiveSessionForParameterRequest(session.Parameters.Id);
                var handler = new GetActiveSessionForParameterHandler(context, Messaging);

                await ShouldReturnValidationFailure(handler.Handle(request, CancellationToken), ErrorMessages.ParameterHasNoActiveSessions);
            }
        }
    }
}
