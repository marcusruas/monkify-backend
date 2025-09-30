using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Sessions;
using Moq;

namespace Monkify.Tests.UnitTests.Shared
{
    public class SessionServiceTestFactory
    {
        public SessionServiceTestFactory()
        {
            FakerPTBR = new Faker();

            var contextOptions = new DbContextOptionsBuilder<MonkifyDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;

            DbContext = new(contextOptions);

            var hubClientsMock = new Mock<IHubClients>();
            var clientProxyMock = new Mock<IClientProxy>();

            _hub = new Mock<IHubContext<ActiveSessionsHub>>();
            _hub.Setup(x => x.Clients).Returns(hubClientsMock.Object);
            hubClientsMock.Setup(x => x.All).Returns(clientProxyMock.Object);

            clientProxyMock.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback<string, object[], CancellationToken>((method, args, token) => NumberOfBatches++);
        }

        public MonkifyDbContext DbContext { get; }
        public int NumberOfBatches { get; set; }

        protected readonly Faker FakerPTBR;

         
        private readonly Mock<IHubContext<ActiveSessionsHub>> _hub;
        
        public IEnumerable<Bet> CreateBets(SessionCharacterType charType, int length, int count = 1)
        {
            for (int i = 1; i <= count; i++)
            {
                var charset = charType.StringValueOf();
                yield return new Bet(BetStatus.Made, 10, FakerPTBR.Random.String2(length, charset), Guid.NewGuid().ToString());
            }
        }

        public SessionService Create()
            => new (GenerateSettings(), DbContext, _hub.Object, new ());

        private GeneralSettings GenerateSettings() => new()
        {
            Sessions = new()
            {
                DelayBetweenSessions = 1,
                TerminalBatchLimit = 1000,
                SessionTerminalEndpoint = "terminal/{0}"
            }
        };
    }
}
