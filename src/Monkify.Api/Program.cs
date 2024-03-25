using Monkify.Common.Messaging;
using Serilog;
using Serilog.Sinks.MSSqlServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IMessaging, Messaging>();

AddLogs(builder);

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
    var connectionStringLogsDB = builder.Configuration.GetConnectionString("Logs");

    MSSqlServerSinkOptions logOptions = new ();
    {
        logOptions.AutoCreateSqlTable = true;
        logOptions.TableName = "MonkifyLogs";
    }

    Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.MSSqlServer(connectionStringLogsDB, logOptions)
                .CreateLogger();

    builder.Logging.AddSerilog(Log.Logger);
}