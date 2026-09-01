using System;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monody.Bot;
using Monody.Bot.ModuleBuilder;
using Monody.Services;

// Avoid slow thread injection delaying interaction defers past Discord's 3s window.
ThreadPool.SetMinThreads(Math.Max(Environment.ProcessorCount * 4, 16), Math.Max(Environment.ProcessorCount * 4, 16));

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

// A Discord gateway hiccup can throw inside a DiscordClientService. The default is to stop the
// host, which takes the bot down and loses anything held in memory; log and keep running instead.
builder.Services.Configure<HostOptions>(options =>
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

// Services
builder.Services
    .AddServices(builder.Configuration)
    .AddModulesFromAssembly(builder.Configuration, Assembly.GetExecutingAssembly())
    .AddCache(builder.Configuration)
    .AddDiscord();

// Build and run
var app = builder.Build();
await app.RunAsync();
