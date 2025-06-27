using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Events;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Background.Workers;
using Monkify.Infrastructure.Context;
using Monkify.Tests.UnitTests.Shared;
using Moq;
using Polly;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.UnitTests.Background
{
    public class CreateSessionTests : UnitTestsClass
    {
        public CreateSessionTests()
        {
            _serviceProviderMock = new();
            _serviceScopeMock = new();
            _mediatorMock = new Mock<IMediator>();
            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            serviceScopeFactory.Setup(x => x.CreateScope()).Returns(_serviceScopeMock.Object);

            _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactory.Object);
            _serviceScopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);

            var settings = new GeneralSettings();
            settings.Workers = new() { CreateSessionsInterval = 2 };
            settings.Sessions = new() { ActiveSessionsEndpoint = "endpoint/{0}" };

            _serviceProviderMock.Setup(x => x.GetService(typeof(GeneralSettings))).Returns(settings);
            _mediatorMock.Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>())).Verifiable();
            _serviceProviderMock.Setup(x => x.GetService(typeof(IMediator))).Returns(_mediatorMock.Object);

            var hubContextMock = new Mock<IHubContext<ActiveSessionsHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockAllClientProxy = new Mock<IClientProxy>();

            mockClients.Setup(clients => clients.All).Returns(mockAllClientProxy.Object);
            hubContextMock.Setup(x => x.Clients).Returns(mockClients.Object);

            _serviceProviderMock.Setup(x => x.GetService(typeof(IHubContext<ActiveSessionsHub>))).Returns(hubContextMock.Object);
        }

        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IServiceScope> _serviceScopeMock;
        private readonly Mock<IMediator> _mediatorMock;

        [Fact]
        public async Task CreateSessions_ActiveParameter_ShouldNotCreateSession()
        {
            var parameters = new SessionParameters()
            {
                Name = Faker.Random.Word(),
                Description = Faker.Random.Words(6),
                AcceptDuplicatedCharacters = true,
                ChoiceRequiredLength = 4,
                RequiredAmount = 2,
                AllowedCharacters = SessionCharacterType.Letters,
                PresetChoices = new List<PresetChoice>() { new PresetChoice(Faker.Random.String2(4)), new PresetChoice(Faker.Random.String2(4)), new PresetChoice(Faker.Random.String2(4)) },
                Active = true,
            };

            using (var context = new MonkifyDbContext(ContextOptions))
            {
                context.Add(parameters);
                context.SaveChanges();

                _serviceProviderMock.Setup(x => x.GetService(typeof(MonkifyDbContext))).Returns(context);

                var worker = new CreateSessions(_serviceProviderMock.Object);

                await worker.ExecuteProcess(CancellationToken);

                context.Sessions.Any(x => x.ParametersId == parameters.Id).ShouldBeTrue();
                _mediatorMock.Verify(x => x.Publish(It.IsAny<SessionStartEvent>(), It.IsAny<CancellationToken>()));
            }
        }

        [Fact]
        public async Task CreateSessions_NoParameters_ShouldNotCreateSession()
        {
            using (var context = new MonkifyDbContext(ContextOptions))
            {
                _serviceProviderMock.Setup(x => x.GetService(typeof(MonkifyDbContext))).Returns(context);

                var worker = new CreateSessions(_serviceProviderMock.Object);

                await worker.ExecuteProcess(CancellationToken);

                context.Sessions.Any().ShouldBeFalse();
            }
        }
    }
}
