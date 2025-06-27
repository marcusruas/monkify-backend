using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Monkify.Domain.Configs.ValueObjects;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Background.Workers;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.ResponseTypes.Sessions;
using Monkify.Results;
using Monkify.Tests.IntegrationTests.Shared;
using Monkify.Tests.IntegrationTests.TestData;
using Polly;
using Shouldly;

namespace Monkify.Tests.IntegrationTests
{
    [Collection(nameof(MonkifyTestsCollection))]
    public class MonkifyIntegrationTests : IAsyncLifetime
    {
        private IContainer AppContainer { get; set; }
        private ApplicationFixture Fixture { get; set; }

        private string GetLocalAppContainerUrl() => $"http://localhost:{AppContainer.GetMappedPublicPort(8080)}";

        private readonly DbContextOptions<MonkifyDbContext> DatabaseOptions;

        public MonkifyIntegrationTests(ApplicationFixture fixture)
        {
            Fixture = fixture;
            DatabaseOptions = new DbContextOptionsBuilder<MonkifyDbContext>().UseSqlServer(Fixture.GetLocalSqlServerConnectionString("MONKIFY")).Options;
        }

        [Fact]
        public async Task Api_GetActiveParameters_ShouldReturnActiveParameter()
        {
            using var context = new MonkifyDbContext(DatabaseOptions);
            var parameters = MonkifyIntegrationTestsStubs.GenerateSession();

            await context.SessionParameters.AddAsync(parameters);
            await context.SaveChangesAsync();

            string endpoint = $"/api/sessions/active-types";
            using var client = new HttpClient() { BaseAddress = new Uri(GetLocalAppContainerUrl()) };
            var activeTypesResult = await client.GetAsync(endpoint);

            activeTypesResult.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
            var jsonTypes = await activeTypesResult.Content.ReadAsStringAsync();
            var apiResultBody = JsonDocument.Parse(jsonTypes);

            apiResultBody.RootElement.GetProperty("data").GetArrayLength().ShouldBeGreaterThan(0);
            apiResultBody.RootElement.GetProperty("data")[0].GetProperty("sessionTypeId").GetString().ShouldBe(parameters.Id.ToString());
        }

        [Fact]
        public async Task Api_GetActiveSessionForParameter_ShouldReturnActiveSession()
        {
            using var context = new MonkifyDbContext(DatabaseOptions);
            var parameters = MonkifyIntegrationTestsStubs.GenerateSession();

            await context.SessionParameters.AddAsync(parameters);
            await context.SaveChangesAsync();

            await Task.Delay(5000);

            string endpoint = $"/api/sessions/{parameters.Id}";
            using var client = new HttpClient() { BaseAddress = new Uri(GetLocalAppContainerUrl()) };
            var activeTypesResult = await client.GetAsync(endpoint);

            activeTypesResult.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
            var jsonTypes = await activeTypesResult.Content.ReadAsStringAsync();
            var apiResultBody = JsonDocument.Parse(jsonTypes);

            var sessionId = apiResultBody.RootElement.GetProperty("data").GetProperty("sessionId").GetString();
            sessionId.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task Api_RegisterBet_ShouldInsertBet()
        {
            using var context = new MonkifyDbContext(DatabaseOptions);
            var parameters = MonkifyIntegrationTestsStubs.GenerateSession();

            await context.SessionParameters.AddAsync(parameters);
            await context.SaveChangesAsync();

            await Task.Delay(5000);

            var createdSession = await context.Sessions.FirstOrDefaultAsync(x => x.Status == SessionStatus.WaitingBets && x.ParametersId == parameters.Id);

            createdSession.ShouldNotBeNull();
            createdSession.Id.ShouldNotBe(Guid.Empty);

            string endpoint = $"/api/sessions/{createdSession.Id}/bets";
            using var client = new HttpClient() { BaseAddress = new Uri(GetLocalAppContainerUrl()) };
            var request = new StringContent(MonkifyIntegrationTestsStubs.GenerateBet().AsJson());
            request.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var betResult = await client.PostAsync(endpoint, request);

            betResult.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        }

        //[Fact]
        //public async Task SessionProcessing_NewBets_ShouldRunSession()
        //{
        //    using var context = new MonkifyDbContext(DatabaseOptions);
        //    var parameters = MonkifyIntegrationTestsStubs.GenerateSession();

        //    await context.SessionParameters.AddAsync(parameters);
        //    await context.SaveChangesAsync();

        //    await Task.Delay(5000);

        //    var createdSession = await context.Sessions.FirstOrDefaultAsync(x => x.Status == SessionStatus.WaitingBets && x.ParametersId == parameters.Id);

        //    createdSession.ShouldNotBeNull();

        //    string endpoint = $"/api/sessions/{createdSession.Id}/bets";

        //    var client = new HttpClient() { BaseAddress = new Uri(GetLocalAppContainerUrl()) };
        //    var firstBetResult = await client.PostAsync(endpoint, new StringContent(MonkifyIntegrationTestsStubs.GenerateBet().AsJson(), encoding: Encoding.UTF8, mediaType: "application/json"));

        //    firstBetResult.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        //}

        #region Life cycle methods
        public async Task InitializeAsync()
        {
            var dir = Directory.GetParent(CommonDirectoryPath.GetSolutionDirectory().DirectoryPath).FullName;

            var image = new ImageFromDockerfileBuilder()
                .WithDockerfileDirectory(dir)
                .WithDockerfile("Dockerfile")
                .Build();

            await image.CreateAsync();

            AppContainer = new ContainerBuilder()
                .WithImage(image)
                .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
                .WithName($"{ApplicationFixture.PROJECT_NAME}-app-instance-{Guid.NewGuid()}")
                .WithNetwork(Fixture.NetworkContainer)
                .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
                .WithEnvironment("ConnectionStrings__Monkify", Fixture.GetContainerSqlServerConnectionString("MONKIFY"))
                .WithEnvironment("GeneralSettings__Sessions__MinimumWaitPeriodForBets", "5")
                .WithEnvironment("GeneralSettings__Sessions__TimeUntilSessionStarts", "5")
                .WithEnvironment("GeneralSettings__Sessions__TerminalBatchLimit", "100")
                .WithEnvironment("GeneralSettings__Sessions__DelayBetweenSessions", "5")
                .WithEnvironment("GeneralSettings__Workers__CreateSessionsInterval", "5")
                .WithEnvironment("GeneralSettings__Workers__RefundBetsInterval", "1800")
                .WithEnvironment("GeneralSettings__Workers__RewardSessionsInterval", "1800")
                .WithPortBinding(8080, false)
                .WithAutoRemove(true).WithCleanUp(true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8080))
                .Build();

            await AppContainer.StartAsync();
        }

        public async Task DisposeAsync()
        {
            if (AppContainer != null) await AppContainer.StopAsync();
        }
        #endregion
    }
}
