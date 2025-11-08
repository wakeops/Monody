using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monody.Bot;

ModuleLoader.LoadModules();

var builder = Host.CreateApplicationBuilder(args);

// Logging
builder.Logging
    .ClearProviders()
    .AddConsole();

// Configuration
builder.Configuration
    .AddJsonFile("appsettings.json", true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true)
    .AddEnvironmentVariables();

// Services
builder.Services
    .AddCache(builder.Configuration)
    .AddModules(builder.Configuration)
    .AddDiscord();

// Build and run
var app = builder.Build();
await app.RunAsync();
