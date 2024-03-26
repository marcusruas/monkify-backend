using Monkify.Worker;
using Monkify.Common.Extensions;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using Monkify.Infrastructure.Context;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Monkify.Common.Messaging;

var builder = Host.CreateApplicationBuilder(args);

var monkifyConnectionString = builder.Configuration.GetConnectionString("Monkify");
var logsConnectionString = builder.Configuration.GetConnectionString("Logs");

AddLogs(builder);
builder.Services.AddDbContext<MonkifyDbContext>(options => options.UseSqlServer(monkifyConnectionString));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

void AddLogs(HostApplicationBuilder builder)
{
    MSSqlServerSinkOptions logOptions = new();
    {
        logOptions.AutoCreateSqlTable = true;
        logOptions.TableName = "MonkifyWorkerLogs";
    }

    Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.MSSqlServer(logsConnectionString, logOptions)
                .CreateLogger();

    builder.Logging.AddSerilog(Log.Logger);
}