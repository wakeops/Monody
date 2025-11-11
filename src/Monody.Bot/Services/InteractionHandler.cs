using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monody.Bot.Options;

namespace Monody.Bot.Services;

internal class InteractionHandler : DiscordClientService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly InteractionService _interactionService;
    private readonly DiscordOptions _options;

    public InteractionHandler(DiscordSocketClient client, ILogger<DiscordClientService> logger, IServiceProvider provider, InteractionService interactionService, IOptions<DiscordOptions> options) : base(client, logger)
    {
        _serviceProvider = provider;
        _interactionService = interactionService;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Client.InteractionCreated += HandleInteractionAsync;

        var appAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains("Monody.Module"));
        foreach (var appAssembly in appAssemblies) {
            await _interactionService.AddModulesAsync(appAssembly, _serviceProvider);
        }

        await Client.WaitForReadyAsync(cancellationToken);

        if (_options.GuildId != null)
        {
            await _interactionService.RegisterCommandsToGuildAsync(_options.GuildId.Value, true);
        }
        else
        {
            await _interactionService.RegisterCommandsGloballyAsync(true);
        }
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(Client, interaction);

            var result = await _interactionService.ExecuteCommandAsync(context, _serviceProvider);

            if (!result.IsSuccess && result.Error != InteractionCommandError.UnknownCommand)
            {
                Logger.Log_InteractionFailure(result.Error, result.ErrorReason);

                await context.Interaction.RespondAsync("Something went wrong processing this request.");
            }
        }
        catch (Exception ex)
        {
            Logger.Log_InteractionException(ex);

            if (interaction.Type is InteractionType.ApplicationCommand)
            {
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }
}

internal static partial class InteractionHandlerLoggingExtensions
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Unable to execute interaction: [{Error}] {ErrorReason}")]
    public static partial void Log_InteractionFailure(this ILogger logger, InteractionCommandError? error, string errorReason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception occurred whilst attempting to handle interaction.")]
    public static partial void Log_InteractionException(this ILogger logger, Exception ex);
}
