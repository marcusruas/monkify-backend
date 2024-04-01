using Monkify.Common.Extensions;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Monkify.Common.Messaging;
using Monkify.Infrastructure.Handlers.Sessions.Workers;
using MassTransit;
using Monkify.Infrastructure;
using Monkify.Infrastructure.Context;

var builder = Host.CreateApplicationBuilder(args);

var monkifyConnectionString = builder.Configuration.GetConnectionString("Monkify");
var logsConnectionString = builder.Configuration.GetConnectionString("Logs");

AddLogs(builder);
builder.Services.AddDbContext<MonkifyDbContext>(options => options.UseSqlServer(monkifyConnectionString));
builder.Services.AddMassTransit(x =>
{
    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});
builder.Services.AddHandlers();

builder.Services.AddHostedService<OpenSessions>();

var host = builder.Build();
host.Run();

void AddLogs(HostApplicationBuilder builder)
{
    MSSqlServerSinkOptions logOptions = new();
    {
        logOptions.AutoCreateSqlTable = true;
        logOptions.TableName = "MonkifyWorkerLogs";
    }

    var logBuilder = new LoggerConfiguration();

    if (builder.Environment.IsDevelopment())
        logBuilder = logBuilder.MinimumLevel.Warning();
    else
        logBuilder = logBuilder.MinimumLevel.Error();

    logBuilder = logBuilder.WriteTo.MSSqlServer(logsConnectionString, logOptions);

    Log.Logger = logBuilder.CreateLogger();

    builder.Logging.AddSerilog(Log.Logger);
}