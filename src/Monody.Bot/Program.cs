using System;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monody.Bot;
using Monody.Bot.ModuleBuilder;
using Monody.Services;

// The default ThreadPool only injects new worker threads gradually (roughly one every
// 500ms once the queue backs up), which can delay scheduling of interaction handlers
// past Discord's 3 second defer window under a burst of gateway activity or on a
// CPU-limited container. Raise the floor so bursts don't wait on thread injection.
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

// Services
builder.Services
    .AddServices(builder.Configuration)
    .AddModulesFromAssembly(builder.Configuration, Assembly.GetExecutingAssembly())
    .AddCache(builder.Configuration)
    .AddDiscord();

// Build and run
var app = builder.Build();
await app.RunAsync();
