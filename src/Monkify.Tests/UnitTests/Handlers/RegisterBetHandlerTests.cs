using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Monkify.Common.Extensions;
using Monkify.Common.Resources;
using Monkify.Common.Results;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Handlers.Sessions.RegisterBet;
using Monkify.Infrastructure.Services.Solana;
using Monkify.Tests.UnitTests.Shared;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.UnitTests.Handlers
{
    public class RegisterBetHandlerTests : UnitTestsClass
    {
        public RegisterBetHandlerTests()
        {
            _solanaServiceMock = new();
            _hubContextMock = new();
            _settings = new GeneralSettings();
            _settings.Sessions = new SessionSettings()
            {
                SessionBetsEndpoint = "endpoint/{0}",
            };
            _settings.Token = new TokenSettings()
            {
                Decimals = 6
            };

            _hubContextMock = new Mock<IHubContext<RecentBetsHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockAllClientProxy = new Mock<IClientProxy>();

            mockClients.Setup(clients => clients.All).Returns(mockAllClientProxy.Object);
            _hubContextMock.Setup(x => x.Clients).Returns(mockClients.Object);
        }

        private readonly GeneralSettings _settings;
        private readonly Mock<ISolanaService> _solanaServiceMock;
        private readonly Mock<IHubContext<RecentBetsHub>> _hubContextMock;

        [Theory]
        [InlineData(SessionStatus.WaitingBets)]
        [InlineData(SessionStatus.SessionStarting)]
        public async Task RegisterBet_CorrectData_ShouldReturnSuccess(SessionStatus status)
        {
            var requestBody = new RegisterBetRequestBody()
            {
                Amount = 2,
                Seed = Faker.Random.String2(40),
                Choice = "test",
                PaymentSignature = Faker.Random.String2(88),
                Wallet = Faker.Random.String2(40)
            };
            var session = new Session();
            session.Status = status;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), Description = Faker.Random.Words(6), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, AllowedCharacters = SessionCharacterType.Letters };
            _solanaServiceMock.Setup(x => x.ValidateBetPayment(It.IsAny<Bet>())).Returns(Task.FromResult(new ValidationResult()));

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var request = new RegisterBetRequest(session.Id, requestBody);
                var handler = new RegisterBetHandler(context, Messaging, _hubContextMock.Object, _settings, _solanaServiceMock.Object, new());

                var result = await handler.HandleRequest(request, CancellationToken);

                result.ShouldNotBeNull();
                Messaging.HasErrors().ShouldBeFalse();
                Messaging.HasValidationFailures().ShouldBeFalse();
            }
        }

        [Fact]
        public async Task RegisterBet_SessionNotFound_ShouldReturnError()
        {
            var requestBody = new RegisterBetRequestBody()
            {
                Amount = 2,
                Choice = "test",
                PaymentSignature = Faker.Random.String2(88),
                Wallet = Faker.Random.String2(40)
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                var request = new RegisterBetRequest(Guid.NewGuid(), requestBody);
                var handler = new RegisterBetHandler(context, Messaging, _hubContextMock.Object, _settings, _solanaServiceMock.Object, new());
                _solanaServiceMock.Setup(x => x.ValidateBetPayment(It.IsAny<Bet>())).Returns(Task.FromResult(new ValidationResult()));

                string expectedMessage = ErrorMessages.SessionNotValidForBets + " " + ErrorMessages.RefundWarning;
                await ShouldReturnValidationFailure(handler.HandleRequest(request, CancellationToken), expectedMessage);
            }
        }

        [Fact]
        public async Task RegisterBet_SessionInWrongStatus_ShouldReturnError()
        {
            var requestBody = new RegisterBetRequestBody()
            {
                Amount = 2,
                Choice = "test",
                PaymentSignature = Faker.Random.String2(88),
                Wallet = Faker.Random.String2(40)
            };
            var session = new Session();
            session.Status = SessionStatus.RewardForWinnersCompleted;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), Description = Faker.Random.Words(6), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, AllowedCharacters = SessionCharacterType.Letters };
            _solanaServiceMock.Setup(x => x.ValidateBetPayment(It.IsAny<Bet>())).Returns(Task.FromResult(new ValidationResult()));

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var request = new RegisterBetRequest(session.Id, requestBody);
                var handler = new RegisterBetHandler(context, Messaging, _hubContextMock.Object, _settings, _solanaServiceMock.Object, new());

                string expectedMessages = ErrorMessages.SessionNotValidForBets + " " + ErrorMessages.RefundWarning;
                await ShouldReturnValidationFailure(handler.HandleRequest(request, CancellationToken), expectedMessages);
            }
        }

        [Fact]
        public async Task RegisterBet_InvalidBetChoice_ShouldReturnError()
        {
            var requestBody = new RegisterBetRequestBody()
            {
                Amount = 2,
                Choice = "test",
                PaymentSignature = Faker.Random.String2(88),
                Wallet = Faker.Random.String2(40)
            };
            var session = new Session();
            session.Status = SessionStatus.WaitingBets;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), Description = Faker.Random.Words(6), AcceptDuplicatedCharacters = false, ChoiceRequiredLength = 4, RequiredAmount = 2, AllowedCharacters = SessionCharacterType.Letters };
            _solanaServiceMock.Setup(x => x.ValidateBetPayment(It.IsAny<Bet>())).Returns(Task.FromResult(new ValidationResult()));

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var request = new RegisterBetRequest(session.Id, requestBody);
                var handler = new RegisterBetHandler(context, Messaging, _hubContextMock.Object, _settings, _solanaServiceMock.Object, new());
                string expectedMessage = BetValidationResult.UnacceptedDuplicateCharacters.StringValueOf() + " " + ErrorMessages.RefundWarning;

                await ShouldReturnValidationFailure(handler.HandleRequest(request, CancellationToken), expectedMessage);
            }
        }

        [Fact]
        public async Task RegisterBet_InvalidSignature_ShouldReturnError()
        {
            var paymentSignature = Faker.Random.String2(88);

            var requestBody = new RegisterBetRequestBody()
            {
                Amount = 2,
                Choice = "test",
                PaymentSignature = paymentSignature,
                Wallet = Faker.Random.String2(40)
            };
            var session = new Session();
            session.Bets.Add(new Bet(session.Id, Faker.Random.String2(40), paymentSignature, Faker.Random.String2(40), "test", 2));
            session.Status = SessionStatus.WaitingBets;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), Description = Faker.Random.Words(6), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, AllowedCharacters = SessionCharacterType.Letters };
            _solanaServiceMock.Setup(x => x.ValidateBetPayment(It.IsAny<Bet>())).Returns(Task.FromResult(new ValidationResult()));

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var request = new RegisterBetRequest(session.Id, requestBody);
                var handler = new RegisterBetHandler(context, Messaging, _hubContextMock.Object, _settings, _solanaServiceMock.Object, new());

                await ShouldReturnValidationFailure(handler.HandleRequest(request, CancellationToken), ErrorMessages.PaymentSignatureHasBeenUsed);
            }
        }

        [Fact]
        public async Task RegisterBet_InvalidPayment_ShouldReturnError()
        {
            string errorMessage = Faker.Lorem.Word();

            var requestBody = new RegisterBetRequestBody()
            {
                Amount = 2,
                Choice = "test",
                PaymentSignature = Faker.Random.String2(88),
                Wallet = Faker.Random.String2(40)
            };
            var session = new Session();
            session.Status = SessionStatus.WaitingBets;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), Description = Faker.Random.Words(6), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, AllowedCharacters = SessionCharacterType.Letters };
            _solanaServiceMock.Setup(x => x.ValidateBetPayment(It.IsAny<Bet>())).Returns(Task.FromResult(new ValidationResult(errorMessage)));

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var request = new RegisterBetRequest(session.Id, requestBody);
                var handler = new RegisterBetHandler(context, Messaging, _hubContextMock.Object, _settings, _solanaServiceMock.Object, new());

                await ShouldReturnValidationFailure(handler.HandleRequest(request, CancellationToken), errorMessage);
            }
        }
    }
}
