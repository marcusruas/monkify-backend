using MediatR;
using Microsoft.EntityFrameworkCore;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Background.Events.RewardWinners;
using Monkify.Infrastructure.Background.Events.StartSession;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Sessions;
using Monkify.Tests.UnitTests.Shared;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Monkify.Tests.UnitTests.Background
{
    public class StartSessionHandlerTests : UnitTestsClass, IDisposable
    {
        public StartSessionHandlerTests()
        {
            _context = new MonkifyDbContext(ContextOptions);
            _mediatorMock = new Mock<IMediator>();
            _sessionServiceMock = new Mock<ISessionService>();
            
            _settings = new GeneralSettings
            {
                Sessions = new SessionSettings
                {
                    DelayBetweenSessions = 2
                }
            };
        }

        private readonly MonkifyDbContext _context;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<ISessionService> _sessionServiceMock;
        private readonly GeneralSettings _settings;

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task HandleRequest_SessionWithWinners_ShouldCompleteFullWorkflow()
        {
            // Arrange
            var session = CreateSessionWithBets();
            SetSessionStartDate(session, DateTime.UtcNow.AddMilliseconds(100)); // Short delay for testing
            var notification = new StartSessionEvent(session);

            var monkey = new MonkifyTyper(session);
            SetMonkeyProperties(monkey, hasWinners: true, firstChoiceTyped: "abcd");

            var winningBets = session.Bets.Where(x => x.Choice == "abcd");

            _sessionServiceMock.Setup(x => x.RunSession(session, CancellationToken))
                .ReturnsAsync(monkey);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.InProgress, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.Ended, monkey))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateBetStatus(winningBets, BetStatus.NeedsRewarding))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.CreateSession(session.Parameters))
                .Returns(Task.CompletedTask);

            _mediatorMock.Setup(x => x.Publish(It.IsAny<RewardWinnersEvent>(), CancellationToken))
                .Returns(Task.CompletedTask);

            _context.Sessions.Add(session);
            _context.SaveChanges();

            var handler = new StartSessionHandler(_context, _mediatorMock.Object, 
                _sessionServiceMock.Object, _settings, new());

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.InProgress, It.IsAny<MonkifyTyper>()), Times.Once);
            _sessionServiceMock.Verify(x => x.RunSession(session, CancellationToken), Times.Once);
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.Ended, monkey), Times.Once);

            foreach(var bet in winningBets)
            {
                _sessionServiceMock.Verify(x => x.UpdateBetStatus(It.Is<IEnumerable<Bet>>(x => x.Any(y => y.Choice == bet.Choice)), BetStatus.NeedsRewarding), Times.Once);
            }

            
            _mediatorMock.Verify(x => x.Publish(It.IsAny<RewardWinnersEvent>(), CancellationToken), Times.Once);
            _sessionServiceMock.Verify(x => x.CreateSession(session.Parameters), Times.Once);
        }

        [Fact]
        public async Task HandleRequest_SessionWithNoWinners_ShouldNotDeclareWinners()
        {
            // Arrange
            var session = CreateSessionWithBets();
            SetSessionStartDate(session, DateTime.UtcNow.AddMilliseconds(100));
            var notification = new StartSessionEvent(session);

            var monkey = new MonkifyTyper(session);
            SetMonkeyProperties(monkey, hasWinners: false, firstChoiceTyped: "NONE");

            _sessionServiceMock.Setup(x => x.RunSession(session, CancellationToken))
                .ReturnsAsync(monkey);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.InProgress, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.Ended, monkey))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.CreateSession(session.Parameters))
                .Returns(Task.CompletedTask);

            var handler = new StartSessionHandler(_context, _mediatorMock.Object,
                _sessionServiceMock.Object, _settings, new());

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert
            _sessionServiceMock.Verify(x => x.UpdateBetStatus(It.IsAny<IEnumerable<Bet>>(), BetStatus.NeedsRewarding), Times.Never);
            _mediatorMock.Verify(x => x.Publish(It.IsAny<RewardWinnersEvent>(), CancellationToken), Times.Never);
            _sessionServiceMock.Verify(x => x.CreateSession(session.Parameters), Times.Once);
        }

        [Fact]
        public async Task HandleRequest_ValidSession_ShouldPrepareBetsFromDatabase()
        {
            // Arrange
            var session = CreateSessionWithBets();
            SetSessionStartDate(session, DateTime.UtcNow.AddMilliseconds(100));
            var notification = new StartSessionEvent(session);

            var databaseBets = new List<Bet>
            {
                new Bet(session.Id, "seed1", "sig1", "wallet1", "abcd", 2),
                new Bet(session.Id, "seed2", "sig2", "wallet2", "efgh", 3)
            };

            // Add bets to the real database
            await _context.SessionBets.AddRangeAsync(databaseBets);
            await _context.SaveChangesAsync();

            var monkey = new MonkifyTyper(session);
            SetMonkeyProperties(monkey, hasWinners: false, firstChoiceTyped: "NONE");

            _sessionServiceMock.Setup(x => x.RunSession(session, CancellationToken))
                .ReturnsAsync(monkey);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.InProgress, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.Ended, monkey))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.CreateSession(session.Parameters))
                .Returns(Task.CompletedTask);

            var handler = new StartSessionHandler(_context, _mediatorMock.Object,
                _sessionServiceMock.Object, _settings, new());

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert
            session.Bets.Count.ShouldBe(2);
            session.Bets.Any(b => b.Choice == "abcd").ShouldBeTrue();
            session.Bets.Any(b => b.Choice == "efgh").ShouldBeTrue();
        }

        [Fact]
        public async Task HandleRequest_ValidSession_ShouldPublishRewardWinnersEventWithCorrectSession()
        {
            // Arrange
            var session = CreateSessionWithBets();
            SetSessionStartDate(session, DateTime.UtcNow.AddMilliseconds(100));
            var notification = new StartSessionEvent(session);

            var monkey = new MonkifyTyper(session);
            SetMonkeyProperties(monkey, hasWinners: true, firstChoiceTyped: "abcd");

            RewardWinnersEvent capturedEvent = null;
            _mediatorMock.Setup(x => x.Publish(It.IsAny<RewardWinnersEvent>(), CancellationToken))
                .Callback<INotification, CancellationToken>((evt, token) => capturedEvent = evt as RewardWinnersEvent)
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.RunSession(session, CancellationToken))
                .ReturnsAsync(monkey);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.InProgress, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.Ended, monkey))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateBetStatus(It.IsAny<IEnumerable<Bet>>(), BetStatus.NeedsRewarding))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.CreateSession(session.Parameters))
                .Returns(Task.CompletedTask);

            var handler = new StartSessionHandler(_context, _mediatorMock.Object,
                _sessionServiceMock.Object, _settings, new());

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert - Validate RewardWinnersEvent mapping
            capturedEvent.ShouldNotBeNull();
            capturedEvent.Session.ShouldBe(session);
            capturedEvent.Session.Id.ShouldBe(session.Id);
        }

        [Fact]
        public async Task HandleRequest_ValidSession_ShouldCreateNewSessionWithCorrectParameters()
        {
            // Arrange
            var session = CreateSessionWithBets();
            SetSessionStartDate(session, DateTime.UtcNow.AddMilliseconds(100));
            var notification = new StartSessionEvent(session);

            var monkey = new MonkifyTyper(session);
            SetMonkeyProperties(monkey, hasWinners: false, firstChoiceTyped: "NONE");

            SessionParameters capturedParameters = null;
            _sessionServiceMock.Setup(x => x.CreateSession(It.IsAny<SessionParameters>()))
                .Callback<SessionParameters>(p => capturedParameters = p)
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.RunSession(session, CancellationToken))
                .ReturnsAsync(monkey);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.InProgress, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.Ended, monkey))
                .Returns(Task.CompletedTask);

            var handler = new StartSessionHandler(_context, _mediatorMock.Object,
                _sessionServiceMock.Object, _settings, new());

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert - Validate SessionParameters mapping
            capturedParameters.ShouldNotBeNull();
            capturedParameters.ShouldBe(session.Parameters);
            capturedParameters.Name.ShouldBe(session.Parameters.Name);
        }

        [Fact]
        public async Task DeclareWinners_WinnersExistWithMatchingChoice_ShouldUpdateCorrectBets()
        {
            // Arrange
            var session = CreateSessionWithBets();
            SetSessionStartDate(session, DateTime.UtcNow.AddMilliseconds(100));
            var notification = new StartSessionEvent(session);

            var monkey = new MonkifyTyper(session);
            SetMonkeyProperties(monkey, hasWinners: true, firstChoiceTyped: "abcd");

            IEnumerable<Bet> capturedWinningBets = session.Bets.Where(x => x.Choice == "abcd");
            _sessionServiceMock.Setup(x => x.UpdateBetStatus(It.IsAny<IEnumerable<Bet>>(), BetStatus.NeedsRewarding))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.RunSession(session, CancellationToken))
                .ReturnsAsync(monkey);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.InProgress, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.Ended, monkey))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.CreateSession(session.Parameters))
                .Returns(Task.CompletedTask);

            _mediatorMock.Setup(x => x.Publish(It.IsAny<RewardWinnersEvent>(), CancellationToken))
                .Returns(Task.CompletedTask);

            var handler = new StartSessionHandler(_context, _mediatorMock.Object,
                _sessionServiceMock.Object, _settings, new());

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert
            capturedWinningBets.ShouldNotBeNull();
            capturedWinningBets.All(b => b.Choice == "abcd").ShouldBeTrue();
            capturedWinningBets.Count().ShouldBe(1); // Only one bet with choice "abcd"
        }

        private Session CreateSessionWithBets()
        {
            var parameters = new SessionParameters
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                AllowedCharacters = SessionCharacterType.Letters
            };

            var session = new Session(parameters.Id)
            {
                Parameters = parameters,
                Bets = new List<Bet>
                {
                    new Bet(Guid.NewGuid(), "seed1", "sig1", "wallet1", "abcd", 2),
                    new Bet(Guid.NewGuid(), "seed2", "sig2", "wallet2", "EFGH", 3)
                }
            };

            return session;
        }

        private static void SetSessionStartDate(Session session, DateTime startDate)
        {
            var startDateProperty = typeof(Session).GetProperty(nameof(Session.StartDate));
            startDateProperty?.SetValue(session, startDate);
        }

        private static void SetMonkeyProperties(MonkifyTyper monkey, bool hasWinners, string firstChoiceTyped)
        {
            var hasWinnersProperty = typeof(MonkifyTyper).GetProperty(nameof(MonkifyTyper.HasWinners));
            hasWinnersProperty?.SetValue(monkey, hasWinners);

            var firstChoiceTypedProperty = typeof(MonkifyTyper).GetProperty(nameof(MonkifyTyper.FirstChoiceTyped));
            firstChoiceTypedProperty?.SetValue(monkey, firstChoiceTyped);
        }
    }
}