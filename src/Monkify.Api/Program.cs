using Monkify.Api.Filters;
using Monkify.Domain.Configs.Entities;
using Monkify.Infrastructure.Handlers.Sessions.Hubs;
using Monkify.Infrastructure.Handlers.Sessions.Workers;
using static Monkify.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ExceptionFilter>();
    options.Filters.Add<ModelValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.AddLogs("MonkifyApiLogs");
builder.Services.AddDefaultServices(builder.Configuration);

var sessionSettings = new SessionSettings();
builder.Configuration.Bind(nameof(SessionSettings), sessionSettings);
builder.Services.AddSingleton(sessionSettings);

builder.Services.AddHostedService<CreateSessions>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "DevOrigins",
                      policy =>
                      {
                          policy.SetIsOriginAllowed(origin => true) // Permite qualquer origem
                                .AllowAnyMethod()
                                .AllowAnyHeader()
                                .AllowCredentials();
                      });
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

app.UseAuthorization();

app.MapControllers();

app.Run();