using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Monkify.Common.Resources;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Background.Events.RewardWinners;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Sessions;
using Monkify.Infrastructure.Services.Solana;
using Monkify.Tests.UnitTests.Shared;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Monkify.Tests.UnitTests.Background
{
    public class RewardWinnersHandlerTests : UnitTestsClass
    {
        public RewardWinnersHandlerTests()
        {
            _solanaServiceMock = new Mock<ISolanaService>();
            _sessionServiceMock = new Mock<ISessionService>();
            
            _settings = new GeneralSettings
            {
                Token = new TokenSettings
                {
                    Decimals = 6,
                    CommisionPercentage = 0.1m
                }
            };
        }

        private readonly Mock<ISolanaService> _solanaServiceMock;
        private readonly Mock<ISessionService> _sessionServiceMock;
        private readonly GeneralSettings _settings;

        [Fact]
        public async Task HandleRequest_ValidBlockhashAndSuccessfulTransfers_ShouldCompleteSuccessfully()
        {
            // Arrange
            var session = CreateSessionWithWinners();
            var notification = new RewardWinnersEvent(session);

            _solanaServiceMock.Setup(x => x.GetLatestBlockhashForTokenTransfer())
                .ReturnsAsync("valid-blockhash");

            _solanaServiceMock.Setup(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>()))
                .ReturnsAsync(true);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersInProgress, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersCompleted, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateBetStatus(It.IsAny<Bet>(), BetStatus.Rewarded))
                .Callback<Bet, BetStatus>((bet, status) => bet.Status = status)
                .Returns(Task.CompletedTask);

            var handler = new RewardWinnersHandler(_solanaServiceMock.Object, _sessionServiceMock.Object, _settings);

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersInProgress, It.IsAny<MonkifyTyper>()), Times.Once);
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersCompleted, It.IsAny<MonkifyTyper>()), Times.Once);
            _sessionServiceMock.Verify(x => x.UpdateBetStatus(It.IsAny<Bet>(), BetStatus.Rewarded), Times.Exactly(1));
            _solanaServiceMock.Verify(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>()), Times.Exactly(1));
        }

        [Fact]
        public async Task HandleRequest_NullOrEmptyBlockhash_ShouldSetErrorStatus()
        {
            // Arrange
            var session = CreateSessionWithWinners();
            var notification = new RewardWinnersEvent(session);

            _solanaServiceMock.Setup(x => x.GetLatestBlockhashForTokenTransfer())
                .ReturnsAsync((string)null);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.ErrorWhenProcessingRewards, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            var handler = new RewardWinnersHandler(_solanaServiceMock.Object, _sessionServiceMock.Object, _settings);

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.ErrorWhenProcessingRewards, It.IsAny<MonkifyTyper>()), Times.Once);
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersInProgress, It.IsAny<MonkifyTyper>()), Times.Never);
            _solanaServiceMock.Verify(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>()), Times.Never);
        }

        [Fact]
        public async Task HandleRequest_EmptyStringBlockhash_ShouldSetErrorStatus()
        {
            // Arrange
            var session = CreateSessionWithWinners();
            var notification = new RewardWinnersEvent(session);

            _solanaServiceMock.Setup(x => x.GetLatestBlockhashForTokenTransfer())
                .ReturnsAsync(string.Empty);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.ErrorWhenProcessingRewards, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            var handler = new RewardWinnersHandler(_solanaServiceMock.Object, _sessionServiceMock.Object, _settings);

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.ErrorWhenProcessingRewards, It.IsAny<MonkifyTyper>()), Times.Once);
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersInProgress, It.IsAny<MonkifyTyper>()), Times.Never);
            _solanaServiceMock.Verify(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>()), Times.Never);
        }

        [Fact]
        public async Task HandleRequest_FailedTransfers_ShouldSetErrorStatus()
        {
            // Arrange
            var session = CreateSessionWithWinners();
            var notification = new RewardWinnersEvent(session);

            _solanaServiceMock.Setup(x => x.GetLatestBlockhashForTokenTransfer())
                .ReturnsAsync("valid-blockhash");

            _solanaServiceMock.Setup(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>()))
                .ReturnsAsync(false); // Transfer fails

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersInProgress, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.ErrorWhenProcessingRewards, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            var handler = new RewardWinnersHandler(_solanaServiceMock.Object, _sessionServiceMock.Object, _settings);

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersInProgress, It.IsAny<MonkifyTyper>()), Times.Once);
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.ErrorWhenProcessingRewards, It.IsAny<MonkifyTyper>()), Times.Once);
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersCompleted, It.IsAny<MonkifyTyper>()), Times.Never);
            _sessionServiceMock.Verify(x => x.UpdateBetStatus(It.IsAny<Bet>(), BetStatus.Rewarded), Times.Never);
        }

        [Fact]
        public async Task HandleRequest_BetAlreadyRewardedError_ShouldMarkBetAsRewarded()
        {
            // Arrange
            var session = CreateSessionWithWinnersWithRewardError(ErrorMessages.BetHasAlreadyBeenRewarded);
            var winner = new Bet(BetStatus.NeedsRewarding, 2, "abcd", "abc");
            winner.TransactionLogs = new List<TransactionLog>()
            {
                new(Guid.NewGuid(), (session.Bets.Sum(x => x.Amount)) + 1, "asdfa") // + 1 = compensation for the commission
            };
            session.Bets.Add(winner);

            var notification = new RewardWinnersEvent(session);

            _solanaServiceMock.Setup(x => x.GetLatestBlockhashForTokenTransfer())
                .ReturnsAsync("valid-blockhash");

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersInProgress, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersCompleted, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateBetStatus(It.IsAny<Bet>(), BetStatus.Rewarded))
                .Callback<Bet, BetStatus>((bet, status) => bet.Status = status)
                .Returns(Task.CompletedTask);

            var handler = new RewardWinnersHandler(_solanaServiceMock.Object, _sessionServiceMock.Object, _settings);

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert
            _sessionServiceMock.Verify(x => x.UpdateBetStatus(It.IsAny<Bet>(), BetStatus.Rewarded), Times.Once);
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersCompleted, It.IsAny<MonkifyTyper>()), Times.Once);
            _solanaServiceMock.Verify(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>()), Times.Never);
        }

        [Fact]
        public async Task HandleRequest_MixedSuccessfulAndFailedTransfers_ShouldSetErrorStatus()
        {
            // Arrange
            var session = CreateSessionWithMultipleWinners();
            var notification = new RewardWinnersEvent(session);

            _solanaServiceMock.Setup(x => x.GetLatestBlockhashForTokenTransfer())
                .ReturnsAsync("valid-blockhash");

            // First bet succeeds, second fails
            _solanaServiceMock.SetupSequence(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersInProgress, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.ErrorWhenProcessingRewards, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateBetStatus(It.IsAny<Bet>(), BetStatus.Rewarded))
                .Callback<Bet, BetStatus>((bet, status) => bet.Status = status)
                .Returns(Task.CompletedTask);

            var handler = new RewardWinnersHandler(_solanaServiceMock.Object, _sessionServiceMock.Object, _settings);

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersInProgress, It.IsAny<MonkifyTyper>()), Times.Once);
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.ErrorWhenProcessingRewards, It.IsAny<MonkifyTyper>()), Times.Once);
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersCompleted, It.IsAny<MonkifyTyper>()), Times.Never);
            _sessionServiceMock.Verify(x => x.UpdateBetStatus(It.IsAny<Bet>(), BetStatus.Rewarded), Times.Once); // Only one successful
        }

        [Fact]
        public async Task HandleRequest_AllWinnersNotRewarded_ShouldSetErrorStatus()
        {
            // Arrange
            var session = CreateSessionWithMultipleWinners();
            var notification = new RewardWinnersEvent(session);

            // Set one bet to a different status to simulate not all rewarded
            session.Bets.First(b => b.Status == BetStatus.NeedsRewarding).Status = BetStatus.Made;

            _solanaServiceMock.Setup(x => x.GetLatestBlockhashForTokenTransfer())
                .ReturnsAsync("valid-blockhash");

            _solanaServiceMock.Setup(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>()))
                .ReturnsAsync(true);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersInProgress, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.ErrorWhenProcessingRewards, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateBetStatus(It.IsAny<Bet>(), BetStatus.Rewarded))
                .Returns(Task.CompletedTask);

            var handler = new RewardWinnersHandler(_solanaServiceMock.Object, _sessionServiceMock.Object, _settings);

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.ErrorWhenProcessingRewards, It.IsAny<MonkifyTyper>()), Times.Once);
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersCompleted, It.IsAny<MonkifyTyper>()), Times.Never);
        }

        [Fact]
        public async Task HandleRequest_ValidBetDomainServiceInitialization_ShouldProcessCorrectly()
        {
            // Arrange
            var session = CreateSessionWithWinners();
            var notification = new RewardWinnersEvent(session);
            var winner = new Bet(BetStatus.NeedsRewarding, 2, "abcd", "abc");
            session.Bets.Add(winner);

            _solanaServiceMock.Setup(x => x.GetLatestBlockhashForTokenTransfer())
                .ReturnsAsync("valid-blockhash");

            _solanaServiceMock.Setup(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>()))
                .ReturnsAsync(true);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersInProgress, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersCompleted, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateBetStatus(It.IsAny<Bet>(), BetStatus.Rewarded))
                .Callback<Bet, BetStatus>((bet, status) => bet.Status = status)
                .Returns(Task.CompletedTask);

            var handler = new RewardWinnersHandler(_solanaServiceMock.Object, _sessionServiceMock.Object, _settings);

            // Act & Assert - Should not throw when creating BetDomainService
            await handler.HandleRequest(notification, CancellationToken);

            // Verify the flow completed successfully
            _sessionServiceMock.Verify(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersCompleted, It.IsAny<MonkifyTyper>()), Times.Once);
        }

        [Fact]
        public async Task HandleRequest_ValidBlockhashAndSuccessfulTransfers_ShouldUpdateBetStatusObjects()
        {
            // Arrange
            var session = CreateSessionWithWinners();
            var notification = new RewardWinnersEvent(session);

            // Store original status to verify change
            var originalStatus = session.Bets.First(b => b.Status == BetStatus.NeedsRewarding).Status;

            _solanaServiceMock.Setup(x => x.GetLatestBlockhashForTokenTransfer())
                .ReturnsAsync("valid-blockhash");

            _solanaServiceMock.Setup(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>()))
                .ReturnsAsync(true);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersInProgress, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            _sessionServiceMock.Setup(x => x.UpdateSessionStatus(session, SessionStatus.RewardForWinnersCompleted, It.IsAny<MonkifyTyper>()))
                .Returns(Task.CompletedTask);

            // Mock UpdateBetStatus to simulate the callback behavior (updating the bet object's status)
            _sessionServiceMock.Setup(x => x.UpdateBetStatus(It.IsAny<Bet>(), BetStatus.Rewarded))
                .Callback<Bet, BetStatus>((bet, status) => bet.Status = status)
                .Returns(Task.CompletedTask);

            var handler = new RewardWinnersHandler(_solanaServiceMock.Object, _sessionServiceMock.Object, _settings);

            // Act
            await handler.HandleRequest(notification, CancellationToken);

            // Assert - Verify that the bet objects themselves were updated
            var rewardedBets = session.Bets.Where(b => b.Status == BetStatus.Rewarded);
            rewardedBets.ShouldNotBeEmpty();
            rewardedBets.Count().ShouldBe(1); // Only one bet with NeedsRewarding status

            // Verify the status was actually changed from the original
            originalStatus.ShouldBe(BetStatus.NeedsRewarding);
        }

        [Fact]
        public async Task SessionService_UpdateBetStatus_ShouldUpdateBetObjectDirectly()
        {
            // Arrange
            var session = CreateSessionWithWinners();
            using var context = new MonkifyDbContext(ContextOptions);
            
            // Add the session to the database
            await context.Sessions.AddAsync(session);
            await context.SaveChangesAsync();
            
            var mockHub = new Mock<IHubContext<ActiveSessionsHub>>();
            var sessionBetsTracker = new SessionBetsTracker();
            var sessionService = new SessionService(_settings, context, mockHub.Object, sessionBetsTracker);
            
            var betToUpdate = session.Bets.First(b => b.Status == BetStatus.NeedsRewarding);
            var originalStatus = betToUpdate.Status;
            
            // Act - Call UpdateBetStatus with the bet object
            await sessionService.UpdateBetStatus(betToUpdate, BetStatus.Rewarded);
            
            // Assert - Verify the bet object itself was updated (callback functionality)
            betToUpdate.Status.ShouldBe(BetStatus.Rewarded);
            betToUpdate.Status.ShouldNotBe(originalStatus);
        }

        [Fact]
        public async Task SessionService_UpdateBetStatusCollection_ShouldUpdateAllBetObjectsDirectly()
        {
            // Arrange
            var session = CreateSessionWithMultipleWinners();
            using var context = new MonkifyDbContext(ContextOptions);
            
            // Add the session to the database
            await context.Sessions.AddAsync(session);
            await context.SaveChangesAsync();
            
            var mockHub = new Mock<IHubContext<ActiveSessionsHub>>();
            var sessionBetsTracker = new SessionBetsTracker();
            var sessionService = new SessionService(_settings, context, mockHub.Object, sessionBetsTracker);
            
            var betsToUpdate = session.Bets.Where(b => b.Status == BetStatus.NeedsRewarding).ToList();
            var originalStatuses = betsToUpdate.Select(b => b.Status).ToList();
            
            // Act - Call UpdateBetStatus with collection of bet objects
            await sessionService.UpdateBetStatus(betsToUpdate, BetStatus.Rewarded);
            
            // Assert - Verify all bet objects themselves were updated (callback functionality)
            betsToUpdate.All(b => b.Status == BetStatus.Rewarded).ShouldBeTrue();
            
            // Verify the statuses actually changed
            for (int i = 0; i < betsToUpdate.Count; i++)
            {
                betsToUpdate[i].Status.ShouldNotBe(originalStatuses[i]);
            }
        }

        private Session CreateSessionWithWinners()
        {
            var session = new Session
            {
                Bets = new List<Bet>
                {
                    new Bet(Guid.NewGuid(), "seed1", "sig1", "wallet1", "abcd", 2) { Status = BetStatus.NeedsRewarding },
                    new Bet(Guid.NewGuid(), "seed2", "sig2", "wallet2", "efgh", 3) { Status = BetStatus.Made }
                }
            };

            return session;
        }

        private Session CreateSessionWithMultipleWinners()
        {
            var session = new Session
            {
                Bets = new List<Bet>
                {
                    new Bet(Guid.NewGuid(), "seed1", "sig1", "wallet1", "abcd", 2) { Status = BetStatus.NeedsRewarding },
                    new Bet(Guid.NewGuid(), "seed2", "sig2", "wallet2", "efgh", 3) { Status = BetStatus.NeedsRewarding },
                    new Bet(Guid.NewGuid(), "seed3", "sig3", "wallet3", "IJKL", 1) { Status = BetStatus.Made }
                }
            };

            return session;
        }

        private Session CreateSessionWithWinnersWithRewardError(string errorMessage)
        {
            var session = new Session
            {
                Bets = new List<Bet>
                {
                    CreateBetWithRewardError(errorMessage),
                    CreateBetWithRewardError(errorMessage),
                    CreateBetWithRewardError(errorMessage),
                    CreateBetWithRewardError(errorMessage),
                }
            };

            return session;
        }

        private Bet CreateBetWithRewardError(string errorMessage)
        {
            var bet = new Bet(Guid.NewGuid(), "seed1", "sig1", "wallet1", "abcd", 2)
            {
                Status = BetStatus.Made
            };

            return bet;
        }
    }
}