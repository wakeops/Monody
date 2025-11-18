using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monody.Bot;
using Monody.Bot.ModuleBuilder;
using Monody.Bot.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true)
    .AddEnvironmentVariables();

// Logging
builder.Logging
    .ClearProviders()
    .AddBotLogging(builder.Environment, builder.Configuration);

// Services
builder.Services
    .AddModules(builder.Configuration)
    .AddCache(builder.Configuration)
    .AddDiscord();

// Build and run
var app = builder.Build();
await app.RunAsync();
