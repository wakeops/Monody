using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Monody.Discord.Services;

internal class BotStatusService : DiscordClientService
{
    private const string _statusMessage = "Between signal and silence";

    public BotStatusService(DiscordSocketClient client, ILogger<DiscordClientService> logger) : base(client, logger)
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the client to be ready before setting the status
        await Client.WaitForReadyAsync(stoppingToken);

        Logger.LogInformation("Client is ready!");

        await Client.SetActivityAsync(new Game(_statusMessage));
    }
}
