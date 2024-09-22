using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Monkify.Api.Filters;
using Monkify.Domain.Configs.Entities;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Background.Workers;
using Monkify.Infrastructure.Context;
using Serilog;
using Solnet.Rpc;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static Monkify.Infrastructure.DependencyInjection;
using System.Configuration;
using AspNetCoreRateLimit;
using Monkify.Infrastructure.Background.Events;
using Monkify.Infrastructure.Services.Sessions;
using Monkify.Domain.Sessions.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions();
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitingPolicies"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ExceptionFilter>();
    options.Filters.Add<ModelValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(configs =>
{
    configs.SwaggerDoc("v1", new OpenApiInfo { Title = "Monkify.Api", Version = "v1" });
});

builder.AddLogs("MonkifyApiLogs");
builder.Services.AddDefaultServices(builder.Configuration);

var settings = new GeneralSettings();
builder.Configuration.Bind(nameof(GeneralSettings), settings);
builder.Services.AddSingleton(settings);
builder.Services.AddSingleton(provider => ClientFactory.GetClient(settings.Token.ClusterUrl));

builder.Services.AddHostedService<CreateSessions>();
builder.Services.AddHostedService<RefundBets>();
builder.Services.AddHostedService<RewardSessions>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "DevOrigins", policy =>
        {
            policy.SetIsOriginAllowed(origin => true)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
    );
});

var app = builder.Build();

app.UseIpRateLimiting();

ApplyMigrations(app);
CloseOpenSessions(app);
CreateDefaultSessionParameters(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DevOrigins");

app.MapHub<RecentBetsHub>("/Hubs/RecentBets");
app.MapHub<ActiveSessionsHub>("/Hubs/Sessions");

app.UseHttpsRedirection();

app.MapControllers();

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

        var parameters = new SessionParameters()
        {
            Name = "Monkey Race",
            SessionCharacterType = Monkify.Domain.Sessions.ValueObjects.SessionCharacterType.LowerCaseLetter,
            RequiredAmount = 1,
            MinimumNumberOfPlayers = 2,
            ChoiceRequiredLength = 4,
            AcceptDuplicatedCharacters = true,
            Active = true,
        };

        context.AddAsync(parameters);
        context.SaveChanges();
    }
}