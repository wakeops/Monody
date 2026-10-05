using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monody.AI;
using Monody.App;
using Monody.App.Modules.Slop;
using Monody.App.Services;
using Monody.Discord;
using Monody.Services;

// Avoid slow thread injection delaying interaction defers past Discord's 3s window.
ThreadPool.SetMinThreads(Math.Max(Environment.ProcessorCount * 4, 16), Math.Max(Environment.ProcessorCount * 4, 16));

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    ApplicationName = "Monody",
});

// Configuration
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true)
    .AddEnvironmentVariables();

// Logging
builder.Logging.AddLogging(builder.Environment, builder.Configuration);

// A Discord gateway hiccup can throw inside a DiscordClientService. The default is to stop the
// host, which takes the bot down and loses anything held in memory; log and keep running instead.
builder.Services.Configure<HostOptions>(options =>
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

// Services
builder.Services
    .AddMonodyAI(builder.Configuration)
    .AddSingleton<AIChatService>()
    .AddServices(builder.Configuration)
    .AddCache(builder.Configuration)
    .AddDiscord()
    .AddHostedService<ReminderDeliveryService>();

// Build and run
var app = builder.Build();
await app.RunAsync();
