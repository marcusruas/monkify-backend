using Microsoft.EntityFrameworkCore;
using Monkify.Api.Filters;
using Monkify.Common.Extensions;
using Monkify.Common.Messaging;
using Monkify.Domain;
using Monkify.Infrastructure.Context;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using System.Configuration;
using System.Reflection;
using static Monkify.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var monkifyConnectionString = builder.Configuration.GetConnectionString("Monkify");
var logsConnectionString = builder.Configuration.GetConnectionString("Logs");

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ExceptionFilter>();
    options.Filters.Add<ModelValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

AddLogs(builder);
builder.Services.AddScoped<IMessaging, Messaging>();
builder.Services.AddDbContext<MonkifyDbContext>(options => options.UseSqlServer(monkifyConnectionString));
builder.Services.AddHandlers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

void AddLogs(WebApplicationBuilder builder)
{
    MSSqlServerSinkOptions logOptions = new();
    {
        logOptions.AutoCreateSqlTable = true;
        logOptions.TableName = "MonkifyApiLogs";
    }

    Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.MSSqlServer(logsConnectionString, logOptions)
                .CreateLogger();

    builder.Logging.AddSerilog(Log.Logger);
}