using AspNetCoreRateLimit;
using MassTransit;
using Microsoft.OpenApi.Models;
using Monkify.Api.Filters;
using Monkify.Common.Extensions;
using Monkify.Domain.Configs.Entities;
using Monkify.Infrastructure.Abstractions.KafkaHandlers;
using Monkify.Infrastructure.Hubs;
using Monkify.Infrastructure.Consumers.BetPlaced;
using Monkify.Infrastructure.Handlers.Sessions.RegisterBet;
using Solnet.Rpc;
using static Monkify.Common.Extensions.ConfigurationsExtensions;
using static Monkify.Infrastructure.DependencyInjection;
using Monkify.Infrastructure.Consumers.GameSessionProcessor;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddMonkifySettings();

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

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RegisterBetHandler).Assembly));

builder.Services.AddProducer<BetPlacedEvent>(builder.Configuration);

var app = builder.Build();

app.UseIpRateLimiting();

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