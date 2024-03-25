using Microsoft.Extensions.Hosting;
using Serilog.Sinks.MSSqlServer;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Monkify.Common.Extensions
{
    public static class ProjectSetupExtensions
    {
        public static void AddLogs(this IHostApplicationBuilder builder, string tableName)
        {
            var connectionString = builder.Configuration.GetConnectionString("Logs");

            MSSqlServerSinkOptions logOptions = new();
            {
                logOptions.AutoCreateSqlTable = true;
                logOptions.TableName = "MonkifyLogs";
            }

            Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Warning()
                        .WriteTo.MSSqlServer(connectionString, logOptions)
                        .CreateLogger();

            builder.Logging.AddSerilog(Log.Logger);
        }

        public static void AddDbContext<T>(this IHostApplicationBuilder builder) where T : DbContext
        {
            var connectionString = builder.Configuration.GetConnectionString("Monkify");
            builder.Services.AddDbContext<T>(x => x.UseSqlServer(connectionString));
        }
    }
}
