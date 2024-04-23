using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monkify.Common.Messaging;
using Serilog.Sinks.MSSqlServer;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Monkify.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Monkify.Infrastructure.Services.Sessions;
using Monkify.Infrastructure.Services.Solana;
using System.Diagnostics.CodeAnalysis;

namespace Monkify.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public static class DependencyInjection
    {
        public static void AddDefaultServices(this IServiceCollection services, IConfiguration configuration)
        {
            var monkifyConnectionString = configuration.GetConnectionString("Monkify");

            services.AddScoped<IMessaging, Messaging>();
            services.AddScoped<ISolanaService, SolanaService>();
            services.AddScoped<ISessionService, SessionService>();

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            services.AddDbContext<MonkifyDbContext>(options => options.UseSqlServer(monkifyConnectionString));
            services.AddSignalR();
        }

        public static void AddLogs(this IHostApplicationBuilder builder, string tableName)
        {
            var logsConnectionString = builder.Configuration.GetConnectionString("Monkify");

            MSSqlServerSinkOptions logOptions = new();
            {
                logOptions.AutoCreateSqlTable = true;
                logOptions.TableName = tableName;
            }

            var logBuilder = new LoggerConfiguration();

            if (builder.Environment.IsDevelopment())
                logBuilder = logBuilder.MinimumLevel.Information();
            else
                logBuilder = logBuilder.MinimumLevel.Warning();

            logBuilder = logBuilder.WriteTo.MSSqlServer(logsConnectionString, logOptions);

            Log.Logger = logBuilder.CreateLogger();
            builder.Logging.AddSerilog(Log.Logger);
        }
    }
}
