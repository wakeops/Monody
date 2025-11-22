using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Monody.Bot.Utils;
using Serilog.Context;

namespace Monody.Bot.Services;

internal class InteractionHandler : DiscordClientService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly InteractionService _interactionService;
    private readonly CommandLogger _commandLogger;

    public InteractionHandler(DiscordSocketClient client, ILogger<DiscordClientService> logger, IServiceProvider provider, InteractionService interactionService) : base(client, logger)
    {
        _serviceProvider = provider;
        _interactionService = interactionService;

        _commandLogger = new CommandLogger(logger);
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Client.InteractionCreated += HandleInteractionAsync;
        return Task.CompletedTask;
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        using (LogContext.PushProperty("InteractionId", interaction.Id))
        using (LogContext.PushProperty("Guild", new { Id = interaction.GuildId }))
        using (LogContext.PushProperty("Channel", new { interaction.ChannelId, Type = interaction.InteractionChannel?.GetChannelType() } ))
        using (LogContext.PushProperty("User", new { interaction.User.Id, interaction.User.Username }))
        {
            _commandLogger.LogInteraction(interaction);

            try
            {
                var context = new SocketInteractionContext(Client, interaction);

                var result = await _interactionService.ExecuteCommandAsync(context, _serviceProvider);

                if (!result.IsSuccess && result.Error != InteractionCommandError.UnknownCommand)
                {
                    Logger.LogWarning("Unable to execute interaction: [{Error}] {ErrorReason}", result.Error, result.ErrorReason);

                    await context.Interaction.RespondAsync("Something went wrong processing this request.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception occurred whilst attempting to handle interaction.");

                if (interaction.Type is InteractionType.ApplicationCommand)
                {
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
                }
            }
        }
    }
}
