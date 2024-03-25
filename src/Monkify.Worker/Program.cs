using Monkify.Worker;
using Monkify.Common.Extensions;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using Monkify.Infrastructure.Context;

var builder = Host.CreateApplicationBuilder(args);

builder.AddLogs("MonkifyWorkerLogs");
builder.AddDbContext<MonkifyDbContext>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();