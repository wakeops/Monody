using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monody.Bot;
using Monody.Bot.Services;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

var builder = Host.CreateApplicationBuilder(args);

// Logging
var loggerConfig = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Monody")
    .MinimumLevel.Override("ZiggyCreatures.Caching.Fusion", LogEventLevel.Error);

if (builder.Environment.IsDevelopment())
{
    loggerConfig.WriteTo.Console();
}
else
{
    loggerConfig.WriteTo.Console(new CompactJsonFormatter());
}

builder.Logging
    .ClearProviders()
    .AddSerilog(loggerConfig.CreateLogger());

// Configuration
builder.Configuration
    .AddJsonFile("appsettings.json", true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true)
    .AddEnvironmentVariables();

// Inject modules
var moduleRoot = AppContext.BaseDirectory;
ModuleLoader.LoadModuleAssemblies(builder.Services, builder.Configuration, moduleRoot);

// Services
builder.Services
    .AddCache(builder.Configuration)
    .AddDiscord()

    .AddHostedService<ModuleLoaderService>();

// Build and run
var app = builder.Build();
await app.RunAsync();
