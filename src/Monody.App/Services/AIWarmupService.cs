using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monody.App.Modules.Slop;

namespace Monody.App.Services;

/// <summary>
/// Builds the AI stack before the gateway connects.
/// </summary>
/// <remarks>
/// Discord.Net constructs a module's dependencies before the command method runs, so the first
/// <c>/slop ask</c> after a start paid for building the Kernel, every plugin and the OpenAI client
/// before it could reach <c>DeferAsync</c> - enough on its own to miss the 3-second window.
/// </remarks>
internal sealed class AIWarmupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIWarmupService> _logger;

    public AIWarmupService(IServiceProvider serviceProvider, ILogger<AIWarmupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        _serviceProvider.GetRequiredService<AIChatService>();

        _logger.LogInformation("AI services warmed up in {ElapsedMilliseconds}ms.", stopwatch.ElapsedMilliseconds);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
