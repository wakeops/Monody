using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Monody.Bot.LogEnrichers;
using Monody.Bot.Utils;
using Serilog.Context;

namespace Monody.Bot.Services;

internal class InteractionHandler : DiscordClientService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly InteractionService _interactionService;
    private readonly InteractionLogger _interactionLogger;

    public InteractionHandler(DiscordSocketClient client, ILogger<InteractionHandler> logger, IServiceProvider provider, InteractionService interactionService) : base(client, logger)
    {
        _serviceProvider = provider;
        _interactionService = interactionService;

        _interactionLogger = new InteractionLogger(logger);
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Client.InteractionCreated += HandleInteractionAsync;
        return Task.CompletedTask;
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        using (LogContext.Push(new SocketInteractionEnricher(interaction)))
        {
            _interactionLogger.LogInteraction(interaction);

            try
            {
                var context = new SocketInteractionContext(Client, interaction);

                var result = await _interactionService.ExecuteCommandAsync(context, _serviceProvider);

                if (!result.IsSuccess)
                {
                    Logger.LogWarning("Unable to execute interaction: [{Error}] {ErrorReason}", result.Error, result.ErrorReason);

                    if (result.Error != InteractionCommandError.UnknownCommand)
                    {
                        await context.Interaction.RespondAsync("Something went wrong processing this request.");
                    }
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
