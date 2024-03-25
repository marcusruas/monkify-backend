using Microsoft.EntityFrameworkCore;
using Monkify.Common.Extensions;
using Monkify.Common.Messaging;
using Monkify.Infrastructure.Context;
using Serilog;
using Serilog.Sinks.MSSqlServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IMessaging, Messaging>();
builder.AddLogs("MonkifyApiLogs");
builder.AddDbContext<MonkifyDbContext>();

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

void AddLogs(IHostApplicationBuilder builder)
{
    
}