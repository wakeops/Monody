using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Monody.Bot.Services;

internal class BotStatusService : DiscordClientService
{
    public BotStatusService(DiscordSocketClient client, ILogger<DiscordClientService> logger) : base(client, logger)
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the client to be ready before setting the status
        await Client.WaitForReadyAsync(stoppingToken);

        Logger.Log_ClientReady();

        await Client.SetActivityAsync(new Game("Between signal and silence"));
    }
}

internal static partial class BotStatusServiceLoggingExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Client is ready!")]
    public static partial void Log_ClientReady(this ILogger logger);
}
