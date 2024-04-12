using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Monkify.Api.Filters;
using Monkify.Domain.Configs.Entities;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Background.Workers;
using Solnet.Rpc;
using System.Text;
using static Monkify.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

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

//builder.Services.AddHostedService<CreateSessions>();
//builder.Services.AddHostedService<RefundSession>();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DevOrigins");

app.MapHub<OpenSessionsHub>("/Hubs/OpenSessions");
app.MapHub<ActiveSessionsHub>("/Hubs/ActiveSessions");

app.UseHttpsRedirection();

app.MapControllers();

app.Run();