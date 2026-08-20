using System;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monody.Bot;
using Monody.Bot.ModuleBuilder;
using Monody.Services;

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
