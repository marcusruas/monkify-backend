using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Monkify.Infrastructure.Context;
using Testcontainers.MsSql;

namespace Monkify.Tests.IntegrationTests.Shared
{
    public class ApplicationFixture : IAsyncLifetime
    {
        public ApplicationFixture()
        {
            NetworkContainer = new NetworkBuilder().WithCleanUp(true).Build();

            SqlServerContainer = new ContainerBuilder()
                .WithPortBinding(1433, true)
                .WithName($"{PROJECT_NAME}-mssql-server-{Guid.NewGuid()}")
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithEnvironment("ACCEPT_EULA", "Y")
                .WithEnvironment("SA_PASSWORD", "StrongPassword#123")
                .WithNetwork(NetworkContainer)
                .WithNetworkAliases("mssql")
                .WithAutoRemove(true).WithCleanUp(true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Recovery is complete. This is an informational message only."))
                .Build();
        }

        public INetwork NetworkContainer { get; private set; }
        public IContainer SqlServerContainer { get; private set; }

        public string GetLocalSqlServerConnectionString(string databaseName = "master") => $"Server=localhost,{SqlServerContainer.GetMappedPublicPort(1433)};Database={databaseName};User Id=sa;Password=StrongPassword#123;TrustServerCertificate=True;";
        public string GetContainerSqlServerConnectionString(string databaseName = "master") => $"Server=mssql,1433;Database={databaseName};User Id=sa;Password=StrongPassword#123;TrustServerCertificate=True;";

        public const string PROJECT_NAME = "monkify-tests";

        public async Task InitializeAsync()
        {
            await NetworkContainer.CreateAsync();

            await SqlServerContainer.StartAsync();

            await CreateSqlServerDatabaseAsync();
        }

        public async Task DisposeAsync()
        {
            if (SqlServerContainer != null) await SqlServerContainer.DisposeAsync();

            if (NetworkContainer != null) await NetworkContainer.DisposeAsync();
        }

        private async Task CreateSqlServerDatabaseAsync()
        {
            var options = new DbContextOptionsBuilder<MonkifyDbContext>()
                .UseSqlServer(GetLocalSqlServerConnectionString("master")).Options;

            var context = new MonkifyDbContext(options);
            await context.Database.ExecuteSqlRawAsync("CREATE DATABASE MONKIFY");
        }
    }
}
