using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Handlers.Sessions.RegisterBet;
using Monkify.Infrastructure.Services.Sessions;
using Monkify.Infrastructure.Services.Solana;
using Monkify.Tests.Shared;
using Moq;
using Shouldly;
using Solnet.Rpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.Services
{
    public class SessionServiceTests : UnitTestsClass
    {
        public SessionServiceTests()
        {
            _settings = new()
            {
                Sessions = new SessionSettings()
                {
                    SessionStatusEndpoint = "endpoint/{0}"
                }
            };

            _hubContextMock = new Mock<IHubContext<ActiveSessionsHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockAllClientProxy = new Mock<IClientProxy>();

            mockClients.Setup(clients => clients.All).Returns(mockAllClientProxy.Object);
            _hubContextMock.Setup(x => x.Clients).Returns(mockClients.Object);
        }


        private readonly GeneralSettings _settings;
        private readonly Mock<IHubContext<ActiveSessionsHub>> _hubContextMock;

        [Fact]
        public async Task UpdateSessionStatus_UpdateSessionToStarted_ShouldReturnSuccess()
        {
            var session = new Session();
            session.Status = SessionStatus.WaitingBets;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var service = new SessionService(_settings, context, _hubContextMock.Object);

                await service.UpdateSessionStatus(session, SessionStatus.InProgress);
                var updatedSession = context.Sessions.Include(x => x.StatusLogs).FirstOrDefault(x => x.Id == session.Id);

                updatedSession.ShouldNotBeNull();
                updatedSession.Status.ShouldBe(SessionStatus.InProgress);
                updatedSession.StatusLogs.Any(x => x.PreviousStatus == SessionStatus.WaitingBets && x.NewStatus == SessionStatus.InProgress).ShouldBeTrue();
            }
        }

        [Fact]
        public async Task UpdateSessionStatus_UpdateSessionToStarting_ShouldReturnSuccess()
        {
            var session = new Session();
            session.Status = SessionStatus.WaitingBets;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var service = new SessionService(_settings, context, _hubContextMock.Object);

                await service.UpdateSessionStatus(session, SessionStatus.SessionStarting);
                var updatedSession = context.Sessions.Include(x => x.StatusLogs).FirstOrDefault(x => x.Id == session.Id);

                updatedSession.ShouldNotBeNull();
                updatedSession.Status.ShouldBe(SessionStatus.SessionStarting);
                updatedSession.StartDate.ShouldNotBeNull();
                updatedSession.StatusLogs.Any(x => x.PreviousStatus == SessionStatus.WaitingBets && x.NewStatus == SessionStatus.SessionStarting).ShouldBeTrue();
            }
        }

        [Fact]
        public async Task UpdateSessionStatus_UpdateSessionToEnded_ShouldReturnSuccess()
        {
            var session = new Session();
            session.Status = SessionStatus.InProgress;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            session.Bets = new List<Bet>()
            {
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "love", 2),
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "amor", 2),
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "baco", 2),
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "tres", 2),
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "atos", 2),
            };
            var typer = new MonkifyTyper(session);

            while (!typer.HasWinners)
                typer.GenerateNextCharacter();

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var service = new SessionService(_settings, context, _hubContextMock.Object);

                await service.UpdateSessionStatus(session, SessionStatus.Ended, typer);
                var updatedSession = context.Sessions.Include(x => x.StatusLogs).FirstOrDefault(x => x.Id == session.Id);

                updatedSession.ShouldNotBeNull();
                updatedSession.WinningChoice = typer.FirstChoiceTyped;
                updatedSession.EndDate.ShouldNotBeNull();
                updatedSession.Status.ShouldBe(SessionStatus.Ended);
                updatedSession.Seed.ShouldNotBeNull();
                updatedSession.Seed.Value.ShouldNotBe(0);
                updatedSession.StatusLogs.Any(x => x.PreviousStatus == SessionStatus.InProgress && x.NewStatus == SessionStatus.Ended).ShouldBeTrue();
            }
        }

        [Fact]
        public async Task UpdateSessionStatus_UpdateSessionToEndedWithoutMonkey_ShouldReturnSuccess()
        {
            var session = new Session();
            session.Status = SessionStatus.WaitingBets;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            session.Bets = new List<Bet>()
            {
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "love", 2),
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "amor", 2),
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "baco", 2),
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "tres", 2),
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "atos", 2),
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var service = new SessionService(_settings, context, _hubContextMock.Object);

                await service.UpdateSessionStatus(session, SessionStatus.NotEnoughPlayersToStart);
                var updatedSession = context.Sessions.Include(x => x.StatusLogs).FirstOrDefault(x => x.Id == session.Id);

                updatedSession.ShouldNotBeNull();
                updatedSession.Status.ShouldBe(SessionStatus.NotEnoughPlayersToStart);
                updatedSession.Seed.ShouldBeNull();
                updatedSession.EndDate.ShouldNotBeNull();
                updatedSession.StatusLogs.Any(x => x.PreviousStatus == SessionStatus.WaitingBets && x.NewStatus == SessionStatus.NotEnoughPlayersToStart).ShouldBeTrue();
            }
        }

        [Fact]
        public async Task UpdateSessionStatus_UpdateNonExistingSession_ShouldReturnSuccess()
        {
            var session = new Session();
            session.Status = SessionStatus.WaitingBets;
            session.Parameters = new SessionParameters() { AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                var service = new SessionService(_settings, context, _hubContextMock.Object);

                await service.UpdateSessionStatus(session, SessionStatus.InProgress);
                var updatedSession = context.Sessions.Include(x => x.StatusLogs).FirstOrDefault(x => x.Id == session.Id);

                updatedSession.ShouldBeNull();
            }
        }

        [Fact]
        public async Task UpdateBetStatus_UpdateExistingBets_ShouldReturnSuccess()
        {
            var session = new Session();
            session.Status = SessionStatus.WaitingBets;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            session.Bets = new List<Bet>()
            {
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "love", 2),
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "amor", 2),
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "baco", 2),
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "tres", 2),
                new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "atos", 2),
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var service = new SessionService(_settings, context, _hubContextMock.Object);

                await service.UpdateBetStatus(session.Bets, BetStatus.Refunded);
                var bets = context.SessionBets.Include(x => x.StatusLogs).Where(x => x.SessionId == session.Id);

                foreach (var bet in session.Bets)
                {
                    bet.Status.ShouldBe(BetStatus.Refunded);
                    bet.StatusLogs.Any(x => x.PreviousStatus == BetStatus.Made && x.NewStatus == BetStatus.Refunded).ShouldBeTrue();
                }
            }
        }

        [Fact]
        public async Task UpdateBetStatus_UpdateNonExistingBets_ShouldReturnSuccess()
        {
            var bets = new List<Bet>()
            {
                new (Guid.NewGuid(), Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "love", 2),
                new (Guid.NewGuid(), Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "amor", 2),
                new (Guid.NewGuid(), Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "baco", 2),
                new (Guid.NewGuid(), Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "tres", 2),
                new (Guid.NewGuid(), Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "atos", 2),
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                var service = new SessionService(_settings, context, _hubContextMock.Object);

                await service.UpdateBetStatus(bets, BetStatus.Refunded);

                context.SessionBets.Any().ShouldBeFalse();
            }
        }

        [Fact]
        public async Task UpdateBetStatus_UpdateExistingBet_ShouldReturnSuccess()
        {
            var session = new Session();
            session.Status = SessionStatus.WaitingBets;
            session.Parameters = new SessionParameters() { Name = Faker.Random.Word(), AcceptDuplicatedCharacters = true, ChoiceRequiredLength = 4, RequiredAmount = 2, SessionCharacterType = SessionCharacterType.LowerCaseLetter };
            session.Bets = new List<Bet>()
                {
                    new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "love", 2),
                    new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "amor", 2),
                    new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "baco", 2),
                    new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "tres", 2),
                    new (session.Id, Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "atos", 2),
                };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(session);
                context.SaveChanges();

                var selectedBet = session.Bets.FirstOrDefault();

                var service = new SessionService(_settings, context, _hubContextMock.Object);

                await service.UpdateBetStatus(selectedBet, BetStatus.Refunded);
                var updatedBet = context.SessionBets.Include(x => x.StatusLogs).FirstOrDefault(x => x.Id == selectedBet.Id);

                updatedBet.Status.ShouldBe(BetStatus.Refunded);
                updatedBet.StatusLogs.Any(x => x.PreviousStatus == BetStatus.Made && x.NewStatus == BetStatus.Refunded).ShouldBeTrue();
            }
        }

        [Fact]
        public async Task UpdateBetStatus_NonExistingBet_ShouldReturnSuccess()
        {
            Bet bet = new(Guid.NewGuid(), Faker.Random.String2(40), Faker.Random.String2(88), Faker.Random.String2(44), "love", 2);

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                var service = new SessionService(_settings, context, _hubContextMock.Object);

                await service.UpdateBetStatus(bet, BetStatus.Refunded);
                var updatedBet = context.SessionBets.Include(x => x.StatusLogs).FirstOrDefault(x => x.Id == bet.Id);

                updatedBet.ShouldBeNull();
            }
        }
    }
}
