using Microsoft.EntityFrameworkCore;
using Monkify.Common.Resources;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Handlers.Sessions.GetActiveSessionForParameter;
using Monkify.Tests.UnitTests.Shared;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Monkify.Tests.UnitTests.Handlers
{
    public class GetActiveSessionForParameterHandlerTests : UnitTestsClass
    {
        [Fact]
        public async Task HandleRequest_ValidParameterWithActiveSession_ShouldReturnActiveSessionDto()
        {
            var parameters = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                AllowedCharacters = SessionCharacterType.Letters,
                Active = true
            };

            var session = new Session(parameters.Id);
            session.Status = SessionStatus.WaitingBets;
            session.WinningChoice = Faker.Random.Word();
            session.Bets.Add(new Bet(session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(40), "test", 2));

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.Add(parameters);
                context.Sessions.Add(session);
                context.SaveChanges();

                var request = new GetActiveSessionForParameterRequest(parameters.Id);
                var handler = new GetActiveSessionForParameterHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.SessionId.ShouldBe(session.Id);
                result.Status.ShouldBe(session.Status);
                result.WinningChoice.ShouldBe(session.WinningChoice);
                result.Bets.ShouldNotBeNull();
                result.Bets.Count().ShouldBe(1);

                Messaging.HasErrors().ShouldBeFalse();
                Messaging.HasValidationFailures().ShouldBeFalse();
            }
        }

        [Theory]
        [InlineData(SessionStatus.WaitingBets)]
        [InlineData(SessionStatus.SessionStarting)]
        [InlineData(SessionStatus.InProgress)]
        [InlineData(SessionStatus.Ended)]
        public async Task HandleRequest_ValidParameterWithSessionInProgressStatus_ShouldReturnActiveSessionDto(SessionStatus sessionStatus)
        {
            var parameters = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                AllowedCharacters = SessionCharacterType.Letters,
                Active = true
            };

            var session = new Session(parameters.Id);
            session.Status = sessionStatus;

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.Add(parameters);
                context.Sessions.Add(session);
                context.SaveChanges();

                var request = new GetActiveSessionForParameterRequest(parameters.Id);
                var handler = new GetActiveSessionForParameterHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.SessionId.ShouldBe(session.Id);
                result.Status.ShouldBe(session.Status);

                Messaging.HasErrors().ShouldBeFalse();
                Messaging.HasValidationFailures().ShouldBeFalse();
            }
        }

        [Fact]
        public async Task HandleRequest_ValidParameterWithMultipleSessions_ShouldReturnFirstActiveSession()
        {
            var parameters = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                AllowedCharacters = SessionCharacterType.Letters,
                Active = true
            };

            var activeSession = new Session(parameters.Id);
            activeSession.Status = SessionStatus.InProgress;

            var inactiveSession = new Session(parameters.Id);
            inactiveSession.Status = SessionStatus.RewardForWinnersCompleted;

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.Add(parameters);
                context.Sessions.Add(inactiveSession);
                context.Sessions.Add(activeSession);
                context.SaveChanges();

                var request = new GetActiveSessionForParameterRequest(parameters.Id);
                var handler = new GetActiveSessionForParameterHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.SessionId.ShouldBe(activeSession.Id);
                result.Status.ShouldBe(activeSession.Status);

                Messaging.HasErrors().ShouldBeFalse();
                Messaging.HasValidationFailures().ShouldBeFalse();
            }
        }

        [Fact]
        public async Task HandleRequest_ParameterNotFound_ShouldReturnValidationFailure()
        {
            var nonExistentParameterId = Guid.NewGuid();

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                var request = new GetActiveSessionForParameterRequest(nonExistentParameterId);
                var handler = new GetActiveSessionForParameterHandler(context, Messaging);

                await ShouldReturnValidationFailure(handler.HandleRequest(request, CancellationToken), ErrorMessages.ParameterNotFound);
            }
        }

        [Fact]
        public async Task HandleRequest_InactiveParameter_ShouldReturnValidationFailure()
        {
            var parameters = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                AllowedCharacters = SessionCharacterType.Letters,
                Active = false
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.Add(parameters);
                context.SaveChanges();

                var request = new GetActiveSessionForParameterRequest(parameters.Id);
                var handler = new GetActiveSessionForParameterHandler(context, Messaging);

                await ShouldReturnValidationFailure(handler.HandleRequest(request, CancellationToken), ErrorMessages.ParameterNotFound);
            }
        }

        [Fact]
        public async Task HandleRequest_ParameterWithNoActiveSessions_ShouldReturnValidationFailure()
        {
            var parameters = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                AllowedCharacters = SessionCharacterType.Letters,
                Active = true
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.Add(parameters);
                context.SaveChanges();

                var request = new GetActiveSessionForParameterRequest(parameters.Id);
                var handler = new GetActiveSessionForParameterHandler(context, Messaging);

                await ShouldReturnValidationFailure(handler.HandleRequest(request, CancellationToken), ErrorMessages.ParameterHasNoActiveSessions);
            }
        }

        [Theory]
        [InlineData(SessionStatus.RewardForWinnersCompleted)]
        [InlineData(SessionStatus.NotEnoughPlayersToStart)]
        [InlineData(SessionStatus.SessionEndedAbruptely)]
        public async Task HandleRequest_ParameterWithOnlyInactiveSessions_ShouldReturnValidationFailure(SessionStatus inactiveStatus)
        {
            var parameters = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                AllowedCharacters = SessionCharacterType.Letters,
                Active = true
            };

            var inactiveSession = new Session(parameters.Id);
            inactiveSession.Status = inactiveStatus;

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.Add(parameters);
                context.Sessions.Add(inactiveSession);
                context.SaveChanges();

                var request = new GetActiveSessionForParameterRequest(parameters.Id);
                var handler = new GetActiveSessionForParameterHandler(context, Messaging);

                await ShouldReturnValidationFailure(handler.HandleRequest(request, CancellationToken), ErrorMessages.ParameterHasNoActiveSessions);
            }
        }

        [Fact]
        public async Task HandleRequest_ValidParameterWithActiveSessionAndBets_ShouldIncludeBetsInDto()
        {
            var parameters = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                AllowedCharacters = SessionCharacterType.Letters,
                Active = true
            };

            var session = new Session(parameters.Id);
            session.Status = SessionStatus.WaitingBets;
            
            var bet1 = new Bet(session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(40), "test1", 2);
            var bet2 = new Bet(session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(40), "test2", 3);
            
            session.Bets.Add(bet1);
            session.Bets.Add(bet2);

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.Add(parameters);
                context.Sessions.Add(session);
                context.SaveChanges();

                var request = new GetActiveSessionForParameterRequest(parameters.Id);
                var handler = new GetActiveSessionForParameterHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.SessionId.ShouldBe(session.Id);
                result.Bets.ShouldNotBeNull();
                result.Bets.Count().ShouldBe(2);
                
                var betDtos = result.Bets.ToList();
                betDtos.Any(b => b.Choice == "test1" && b.Amount == 2).ShouldBeTrue();
                betDtos.Any(b => b.Choice == "test2" && b.Amount == 3).ShouldBeTrue();

                Messaging.HasErrors().ShouldBeFalse();
                Messaging.HasValidationFailures().ShouldBeFalse();
            }
        }

        [Fact]
        public async Task HandleRequest_ValidParameterWithActiveSessionNoBets_ShouldReturnEmptyBetsList()
        {
            var parameters = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                AllowedCharacters = SessionCharacterType.Letters,
                Active = true
            };

            var session = new Session(parameters.Id);
            session.Status = SessionStatus.WaitingBets;

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.SessionParameters.Add(parameters);
                context.Sessions.Add(session);
                context.SaveChanges();

                var request = new GetActiveSessionForParameterRequest(parameters.Id);
                var handler = new GetActiveSessionForParameterHandler(context, Messaging);

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                result.SessionId.ShouldBe(session.Id);
                result.Bets.ShouldNotBeNull();
                result.Bets.Count().ShouldBe(0);

                Messaging.HasErrors().ShouldBeFalse();
                Messaging.HasValidationFailures().ShouldBeFalse();
            }
        }
    }
}