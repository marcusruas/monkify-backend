using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Background.Workers;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Sessions;
using Monkify.Infrastructure.Services.Solana;
using Monkify.Tests.Shared;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.Background
{
    public class RefundBetsTests : UnitTestsClass
    {
        public RefundBetsTests()
        {
            _serviceProviderMock = new();
            _serviceScopeMock = new();
            _mediatorMock = new();
            _solanaServiceMock = new();
            _sessionServiceMock = new();
            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            serviceScopeFactory.Setup(x => x.CreateScope()).Returns(_serviceScopeMock.Object);

            _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactory.Object);
            _serviceScopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);

            var settings = new GeneralSettings();
            settings.Workers = new() { RefundBetsInterval = 0 };
            settings.Token = new() { Decimals = 4 };

            _serviceProviderMock.Setup(x => x.GetService(typeof(GeneralSettings))).Returns(settings);
            _mediatorMock.Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>())).Verifiable();
            _serviceProviderMock.Setup(x => x.GetService(typeof(IMediator))).Returns(_mediatorMock.Object);

            _serviceProviderMock.Setup(x => x.GetService(typeof(ISolanaService))).Returns(_solanaServiceMock.Object);
            _serviceProviderMock.Setup(x => x.GetService(typeof(ISessionService))).Returns(_sessionServiceMock.Object);
        }

        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IServiceScope> _serviceScopeMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<ISolanaService> _solanaServiceMock;
        private readonly Mock<ISessionService> _sessionServiceMock;

        [Fact]
        public async Task RefundBets_FailedToGetBlockhash_ShouldReturnWithoutErrors()
        {
            _sessionServiceMock.Setup(x => x.UpdateBetStatus(It.IsAny<Bet>(), It.IsAny<BetStatus>())).Verifiable();
            _solanaServiceMock.Setup(x => x.GetLatestBlockhashForTokenTransfer()).Returns(Task.FromResult((string)null));
            _solanaServiceMock.Setup(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>(), It.IsAny<string>())).Returns(Task.FromResult(true));

            var session = new Session();
            session.Status = SessionStatus.Ended;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            session.Bets = new List<Bet>()
            {
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                _serviceProviderMock.Setup(x => x.GetService(typeof(MonkifyDbContext))).Returns(context);

                var worker = new RefundBets(_serviceProviderMock.Object);

                await worker.ExecuteProcess(CancellationToken);

                _sessionServiceMock.Verify(x => x.UpdateBetStatus(It.IsAny<Bet>(), BetStatus.Refunded), Times.Never());
                _solanaServiceMock.Verify(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>(), It.IsAny<string>()), Times.Never());
            }
        }

        [Fact]
        public async Task RefundBets_ShouldRefundAllBets()
        {
            _sessionServiceMock.Setup(x => x.UpdateBetStatus(It.IsAny<Bet>(), It.IsAny<BetStatus>())).Verifiable();
            _solanaServiceMock.Setup(x => x.GetLatestBlockhashForTokenTransfer()).Returns(Task.FromResult(Faker.Random.String2(40) ?? null));
            _solanaServiceMock.Setup(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>(), It.IsAny<string>())).Returns(Task.FromResult(true));

            var session = new Session();
            session.Status = SessionStatus.Ended;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            session.Bets = new List<Bet>()
            {
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                _serviceProviderMock.Setup(x => x.GetService(typeof(MonkifyDbContext))).Returns(context);

                var worker = new RefundBets(_serviceProviderMock.Object);

                await worker.ExecuteProcess(CancellationToken);

                _sessionServiceMock.Verify(x => x.UpdateBetStatus(It.IsAny<Bet>(), BetStatus.Refunded), Times.Exactly(5));
                _solanaServiceMock.Verify(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>(), It.IsAny<string>()), Times.Exactly(5));
            }
        }

        [Fact]
        public async Task RefundBets_NoBets_ShouldNotRefundAny()
        {
            _sessionServiceMock.Setup(x => x.UpdateBetStatus(It.IsAny<Bet>(), It.IsAny<BetStatus>())).Verifiable();
            _solanaServiceMock.Setup(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>(), It.IsAny<string>())).Returns(Task.FromResult(true));

            var session = new Session();
            session.Status = SessionStatus.Ended;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            session.Bets = new List<Bet>()
            {
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2),
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2),
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                _serviceProviderMock.Setup(x => x.GetService(typeof(MonkifyDbContext))).Returns(context);

                var worker = new RefundBets(_serviceProviderMock.Object);

                await worker.ExecuteProcess(CancellationToken);

                _sessionServiceMock.Verify(x => x.UpdateBetStatus(It.IsAny<Bet>(), BetStatus.Refunded), Times.Never());
                _solanaServiceMock.Verify(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>(), It.IsAny<string>()), Times.Never());
            }
        }

        [Fact]
        public async Task RefundBets_FailToRefundBets_ShouldNotRefundBets()
        {
            _sessionServiceMock.Setup(x => x.UpdateBetStatus(It.IsAny<Bet>(), It.IsAny<BetStatus>())).Verifiable();
            _solanaServiceMock.Setup(x => x.GetLatestBlockhashForTokenTransfer()).Returns(Task.FromResult(Faker.Random.String2(40) ?? null));
            _solanaServiceMock.Setup(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>(), It.IsAny<string>())).Returns(Task.FromResult(false));

            var session = new Session();
            session.Status = SessionStatus.Ended;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            session.Bets = new List<Bet>()
            {
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                _serviceProviderMock.Setup(x => x.GetService(typeof(MonkifyDbContext))).Returns(context);

                var worker = new RefundBets(_serviceProviderMock.Object);

                await worker.ExecuteProcess(CancellationToken);

                _sessionServiceMock.Verify(x => x.UpdateBetStatus(It.IsAny<Bet>(), BetStatus.Refunded), Times.Never());
                _solanaServiceMock.Verify(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>(), It.IsAny<string>()), Times.Exactly(5));
            }
        }

        [Fact]
        public async Task RefundBets_AlreadyRefundedBets_ShouldBeSetAsRefunded()
        {
            _sessionServiceMock.Setup(x => x.UpdateBetStatus(It.IsAny<Bet>(), It.IsAny<BetStatus>())).Verifiable();
            _solanaServiceMock.Setup(x => x.GetLatestBlockhashForTokenTransfer()).Returns(Task.FromResult(Faker.Random.String2(40) ?? null));
            _solanaServiceMock.Setup(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>(), It.IsAny<string>())).Returns(Task.FromResult(true));

            var session = new Session();
            session.Status = SessionStatus.Ended;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            session.Bets = new List<Bet>()
            {
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), Faker.Random.String2(4), 2) { Status = BetStatus.NeedsRefunding },
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);

                foreach(var bet in session.Bets)
                {
                    context.Add(new TransactionLog(bet.Id, 2, Faker.Random.String2(88)));
                }

                context.SaveChanges();

                _serviceProviderMock.Setup(x => x.GetService(typeof(MonkifyDbContext))).Returns(context);

                var worker = new RefundBets(_serviceProviderMock.Object);

                await worker.ExecuteProcess(CancellationToken);

                _sessionServiceMock.Verify(x => x.UpdateBetStatus(It.IsAny<Bet>(), BetStatus.Refunded), Times.Exactly(5));
                _solanaServiceMock.Verify(x => x.TransferTokensForBet(It.IsAny<Bet>(), It.IsAny<BetTransactionAmountResult>(), It.IsAny<string>()), Times.Never());
            }
        }
    }
}
