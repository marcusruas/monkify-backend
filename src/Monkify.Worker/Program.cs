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

builder.AddLogs("MonkifyWorkerLogs");
builder.Services.AddDefaultServices(builder.Configuration);

builder.Services.AddHostedService<OpenSessions>();

var host = builder.Build();
host.Run();