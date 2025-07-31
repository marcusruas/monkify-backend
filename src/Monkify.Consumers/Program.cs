using Microsoft.EntityFrameworkCore;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Infrastructure.Abstractions.KafkaHandlers;
using Monkify.Infrastructure.Background.Workers;
using Monkify.Infrastructure.Consumers.BetPlaced;
using Monkify.Infrastructure.Consumers.GameSessionProcessor;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Sessions;
using Serilog;
using Solnet.Rpc;
using static Monkify.Common.Extensions.ConfigurationsExtensions;
using static Monkify.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions();
builder.Services.AddMemoryCache();
builder.Configuration.AddMonkifySettings();

builder.AddLogs("MonkifyConsumerLogs");
builder.Services.AddDefaultServices(builder.Configuration);

var settings = new GeneralSettings();
builder.Configuration.Bind(nameof(GeneralSettings), settings);
builder.Services.AddSingleton(settings);
builder.Services.AddSingleton(provider => ClientFactory.GetClient(settings.Token.ClusterUrl));


builder.Services.AddHostedService<CreateSessions>();

builder.Services.AddConsumer<BetPlacedEvent, BetPlacedConsumer>(builder.Configuration);
builder.Services.AddProducer<GameSessionProcessorEvent>(builder.Configuration);
builder.Services.AddConsumer<GameSessionProcessorEvent, GameSessionProcessorConsumer>(builder.Configuration);

var app = builder.Build();

ApplyMigrations(app);
CloseOpenSessions(app);

app.Run();

void ApplyMigrations(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MonkifyDbContext>();
            dbContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply migrations of the database.");
        }
    }
}

void CloseOpenSessions(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        var service = scope.ServiceProvider.GetRequiredService<ISessionService>();

        service.CloseOpenSessions();
        service.CreateDefaultSessionParameters();
    }
}