using Microsoft.EntityFrameworkCore;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Infrastructure.Background.Workers;
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
builder.Services.AddHostedService<RefundBets>();
builder.Services.AddHostedService<RewardSessions>();

var app = builder.Build();

ApplyMigrations(app);
CloseOpenSessions(app);
//CreateDefaultSessionParameters(app);

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
    }
}

void CreateDefaultSessionParameters(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<MonkifyDbContext>();

        if (context.SessionParameters.Any())
            return;

        var parameters = new List<SessionParameters>()
        {
            new SessionParameters()
            {
                Name = "Four Letter Race",
                Description = "Type a Four-letter word and hope that Edson types it before anyone else!",
                AllowedCharacters = Monkify.Domain.Sessions.ValueObjects.SessionCharacterType.Letters,
                RequiredAmount = 1,
                MinimumNumberOfPlayers = 2,
                ChoiceRequiredLength = 4,
                AcceptDuplicatedCharacters = true,
                Active = true,
            },
            new SessionParameters()
            {
                Name = "Five Letter Race",
                Description = "Type a Five-letter word and hope that Edson types it before anyone else!",
                AllowedCharacters = Monkify.Domain.Sessions.ValueObjects.SessionCharacterType.Letters,
                RequiredAmount = 1,
                MinimumNumberOfPlayers = 2,
                ChoiceRequiredLength = 5,
                AcceptDuplicatedCharacters = true,
                Active = true,
            },
            new SessionParameters()
            {
                Name = "Six Letter Race",
                Description = "Type a Six-letter word and hope that Edson types it before anyone else!",
                AllowedCharacters = Monkify.Domain.Sessions.ValueObjects.SessionCharacterType.Letters,
                RequiredAmount = 1,
                MinimumNumberOfPlayers = 2,
                ChoiceRequiredLength = 6,
                AcceptDuplicatedCharacters = true,
                Active = true,
            },
            new SessionParameters()
            {
                Name = "Four Number Race",
                Description = "Type a Four number sequence and hope that Edson types it before anyone else!",
                AllowedCharacters = Monkify.Domain.Sessions.ValueObjects.SessionCharacterType.Number,
                RequiredAmount = 1,
                MinimumNumberOfPlayers = 2,
                ChoiceRequiredLength = 4,
                AcceptDuplicatedCharacters = true,
                Active = true,
            },
            new SessionParameters()
            {
                Name = "Six Number Race",
                Description = "Type a Six number sequence and hope that Edson types it before anyone else!",
                AllowedCharacters = Monkify.Domain.Sessions.ValueObjects.SessionCharacterType.Number,
                RequiredAmount = 1,
                MinimumNumberOfPlayers = 2,
                ChoiceRequiredLength = 6,
                AcceptDuplicatedCharacters = true,
                Active = true,
            }
        };

        context.AddRangeAsync(parameters);
        context.SaveChanges();
    }
}