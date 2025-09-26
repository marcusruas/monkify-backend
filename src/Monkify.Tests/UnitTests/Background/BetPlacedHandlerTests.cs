using System;
using System.Reflection;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Background.Events.BetPlaced;
using Monkify.Infrastructure.Background.Events.StartSession;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Services.Sessions;
using Monkify.Tests.UnitTests.Shared;
using Moq;
using Shouldly;

namespace Monkify.Tests.UnitTests.Background
{
    public class BetPlacedHandlerTests : UnitTestsClass
    {
        public BetPlacedHandlerTests()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _serviceScopeMock = new Mock<IServiceScope>();
            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            _sessionBetsTracker = new SessionBetsTracker(); // Use real instance
            _sessionServiceMock = new Mock<ISessionService>();
            _mediatorMock = new Mock<IMediator>();
            _hubContextMock = new Mock<IHubContext<RecentBetsHub>>();
            _hubClientsMock = new Mock<IHubClients>();
            _clientProxyMock = new Mock<IClientProxy>();

            _settings = new GeneralSettings
            {
                Sessions = new SessionSettings
                {
                    SessionBetsEndpoint = "session-bets/{0}",
                    MinimumWaitPeriodForBets = 10
                }
            };

            // Setup service scope factory
            _serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(_serviceScopeMock.Object);
            _serviceScopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);

            // Setup service provider dependencies using GetService instead of GetRequiredService
            _serviceProviderMock.Setup(x => x.GetService(typeof(SessionBetsTracker))).Returns(_sessionBetsTracker);
            _serviceProviderMock.Setup(x => x.GetService(typeof(ISessionService))).Returns(_sessionServiceMock.Object);
            _serviceProviderMock.Setup(x => x.GetService(typeof(IMediator))).Returns(_mediatorMock.Object);
            _serviceProviderMock.Setup(x => x.GetService(typeof(GeneralSettings))).Returns(_settings);
            _serviceProviderMock.Setup(x => x.GetService(typeof(IHubContext<RecentBetsHub>))).Returns(_hubContextMock.Object);

            // Setup SignalR hub
            _hubContextMock.Setup(x => x.Clients).Returns(_hubClientsMock.Object);
            _hubClientsMock.Setup(x => x.All).Returns(_clientProxyMock.Object);
        }

        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IServiceScope> _serviceScopeMock;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
        private readonly SessionBetsTracker _sessionBetsTracker; // Real instance instead of mock
        private readonly Mock<ISessionService> _sessionServiceMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IHubContext<RecentBetsHub>> _hubContextMock;
        private readonly Mock<IHubClients> _hubClientsMock;
        private readonly Mock<IClientProxy> _clientProxyMock;
        private readonly GeneralSettings _settings;

        [Fact]
        public async Task HandleRequest_ValidBetWithEnoughPlayersAndSessionStarts_ShouldCompleteSuccessfully()
        {
            // Arrange
            var session = CreateSessionWithParameters();
            SetSessionCreatedDate(session, DateTime.UtcNow.AddSeconds(-15)); // Minimum time elapsed
            var bet = CreateBet(session);
            var notification = new BetPlacedEvent(bet);

            // Initialize session in tracker
            _sessionBetsTracker.AddSession(session.Id);
            
            // Add another bet to ensure we have enough players (minimum is 2)
            var anotherBet = CreateBet(session);
            anotherBet.Wallet = "different-wallet"; // Ensure different wallet
            anotherBet.Choice = "EFGH"; // Ensure different choice
            await _sessionBetsTracker.AddBetAsync(anotherBet, CancellationToken);

            _sessionServiceMock
                .Setup(x => x.TryStartSession(session))
                .ReturnsAsync(true);

            var handler = new BetPlacedHandler(_serviceScopeFactoryMock.Object);

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert
            _clientProxyMock.Verify(x => x.SendCoreAsync("session-bets/" + session.Id.ToString(), It.IsAny<object[]>(), CancellationToken), Times.Once);
            _sessionServiceMock.Verify(x => x.TryStartSession(session), Times.Once);
            _mediatorMock.Verify(x => x.Publish(It.IsAny<StartSessionEvent>(), CancellationToken), Times.Once);
        }

        [Fact]
        public async Task HandleRequest_ValidBetWithEnoughPlayersButSessionDoesNotStart_ShouldNotPublishStartSessionEvent()
        {
            // Arrange
            var session = CreateSessionWithParameters();
            SetSessionCreatedDate(session, DateTime.UtcNow.AddSeconds(-15)); // Minimum time elapsed
            var bet = CreateBet(session);
            var notification = new BetPlacedEvent(bet);

            // Initialize session in tracker
            _sessionBetsTracker.AddSession(session.Id);
            
            // Add another bet to ensure we have enough players (minimum is 2)
            var anotherBet = CreateBet(session);
            anotherBet.Wallet = "different-wallet"; // Ensure different wallet
            anotherBet.Choice = "EFGH"; // Ensure different choice
            await _sessionBetsTracker.AddBetAsync(anotherBet, CancellationToken);

            _sessionServiceMock
                .Setup(x => x.TryStartSession(session))
                .ReturnsAsync(false);

            var handler = new BetPlacedHandler(_serviceScopeFactoryMock.Object);

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert
            _clientProxyMock.Verify(x => x.SendCoreAsync("session-bets/" + session.Id.ToString(), It.IsAny<object[]>(), CancellationToken), Times.Once);
            _sessionServiceMock.Verify(x => x.TryStartSession(session), Times.Once);
            _mediatorMock.Verify(x => x.Publish(It.IsAny<StartSessionEvent>(), CancellationToken), Times.Never);
        }

        [Fact]
        public async Task HandleRequest_ValidBetButNotEnoughPlayers_ShouldNotTryToStartSession()
        {
            // Arrange
            var session = CreateSessionWithParameters();
            SetSessionCreatedDate(session, DateTime.UtcNow.AddSeconds(-15)); // Minimum time elapsed
            var bet = CreateBet(session);
            var notification = new BetPlacedEvent(bet);

            // Initialize session in tracker but don't add extra bets (only 1 player, minimum is 2)
            _sessionBetsTracker.AddSession(session.Id);

            var handler = new BetPlacedHandler(_serviceScopeFactoryMock.Object);

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert
            _clientProxyMock.Verify(x => x.SendCoreAsync("session-bets/" + session.Id.ToString(), It.IsAny<object[]>(), CancellationToken), Times.Once);
            _sessionServiceMock.Verify(x => x.TryStartSession(It.IsAny<Session>()), Times.Never);
            _mediatorMock.Verify(x => x.Publish(It.IsAny<StartSessionEvent>(), CancellationToken), Times.Never);
        }

        [Fact]
        public async Task HandleRequest_ValidBetButMinimumTimeNotElapsed_ShouldNotTryToStartSession()
        {
            // Arrange
            var session = CreateSessionWithParameters();
            SetSessionCreatedDate(session, DateTime.UtcNow.AddSeconds(-5)); // Minimum time NOT elapsed
            var bet = CreateBet(session);
            var notification = new BetPlacedEvent(bet);

            // Initialize session in tracker
            _sessionBetsTracker.AddSession(session.Id);

            var handler = new BetPlacedHandler(_serviceScopeFactoryMock.Object);

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert
            _clientProxyMock.Verify(x => x.SendCoreAsync("session-bets/" + session.Id.ToString(), It.IsAny<object[]>(), CancellationToken), Times.Once);
            _sessionServiceMock.Verify(x => x.TryStartSession(It.IsAny<Session>()), Times.Never);
            _mediatorMock.Verify(x => x.Publish(It.IsAny<StartSessionEvent>(), CancellationToken), Times.Never);
        }

        [Fact]
        public async Task HandleRequest_ValidBet_ShouldSendBetNotificationEventToWebSocketWithCorrectMapping()
        {
            // Arrange
            var session = CreateSessionWithParameters();
            var bet = CreateBet(session);
            var notification = new BetPlacedEvent(bet);
            
            // Initialize session in tracker
            _sessionBetsTracker.AddSession(session.Id);
            
            object[] capturedArgs = null;
            _clientProxyMock
                .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback<string, object[], CancellationToken>((method, args, token) => capturedArgs = args)
                .Returns(Task.CompletedTask);

            var handler = new BetPlacedHandler(_serviceScopeFactoryMock.Object);

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert - Verify WebSocket endpoint format
            _clientProxyMock.Verify(x => x.SendCoreAsync("session-bets/" + session.Id.ToString(), It.IsAny<object[]>(), CancellationToken), Times.Once);
            
            // Assert - Validate BetNotificationEvent mapping
            capturedArgs.ShouldNotBeNull();
            capturedArgs.Length.ShouldBe(1);
            
            var sentJson = capturedArgs[0] as string;
            sentJson.ShouldNotBeNull();
            
            // Verify the JSON contains all the mapped properties from BetNotificationEvent
            var expectedBetNotification = new BetNotificationEvent(bet.Wallet, bet.PaymentSignature, bet.Amount, bet.Choice);
            sentJson.ShouldContain(bet.Wallet);
            sentJson.ShouldContain(bet.PaymentSignature);
            sentJson.ShouldContain(bet.Amount.ToString());
            sentJson.ShouldContain(bet.Choice);
        }

        [Fact]
        public async Task HandleRequest_ValidBet_ShouldPublishStartSessionEventWithCorrectSession()
        {
            // Arrange
            var session = CreateSessionWithParameters();
            SetSessionCreatedDate(session, DateTime.UtcNow.AddSeconds(-15)); // Minimum time elapsed
            var bet = CreateBet(session);
            var notification = new BetPlacedEvent(bet);

            // Initialize session in tracker
            _sessionBetsTracker.AddSession(session.Id);
            
            // Add another bet to ensure we have enough players (minimum is 2)
            var anotherBet = CreateBet(session);
            anotherBet.Wallet = "different-wallet"; // Ensure different wallet
            anotherBet.Choice = "EFGH"; // Ensure different choice
            await _sessionBetsTracker.AddBetAsync(anotherBet, CancellationToken);

            StartSessionEvent capturedEvent = null;
            _mediatorMock
                .Setup(x => x.Publish(It.IsAny<StartSessionEvent>(), CancellationToken))
                .Callback<INotification, CancellationToken>((evt, token) => capturedEvent = evt as StartSessionEvent)
                .Returns(Task.CompletedTask);

            _sessionServiceMock
                .Setup(x => x.TryStartSession(session))
                .ReturnsAsync(true);

            var handler = new BetPlacedHandler(_serviceScopeFactoryMock.Object);

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert - Validate StartSessionEvent mapping
            capturedEvent.ShouldNotBeNull();
            capturedEvent.Session.ShouldBe(session);
            capturedEvent.Session.Id.ShouldBe(session.Id);
        }

        private Session CreateSessionWithParameters()
        {
            var session = new Session();
            session.Parameters = new SessionParameters
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                AllowedCharacters = SessionCharacterType.Letters,
                MinimumNumberOfPlayers = 2
            };
            return session;
        }

        private Bet CreateBet(Session session)
        {
            return new Bet(
                session.Id,
                Faker.Random.String2(40),
                Faker.Random.String2(88),
                Faker.Random.String2(40),
                Faker.Random.String2(4),
                2
            )
            {
                Session = session
            };
        }

        private static void SetSessionCreatedDate(Session session, DateTime createdDate)
        {
            var createdDateField = typeof(Session).BaseType.GetField("<CreatedDate>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (createdDateField != null)
            {
                createdDateField.SetValue(session, createdDate);
            }
        }
    }
}