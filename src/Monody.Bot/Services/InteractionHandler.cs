using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.Interactions;
using Discord.Rest;
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

        var appAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.StartsWith("Monody.Module"));
        foreach (var appAssembly in appAssemblies)
        {
            await _interactionService.AddModulesAsync(appAssembly, _serviceProvider);
        }

        await Client.WaitForReadyAsync(cancellationToken);

        if (_options.GuildId != null)
        {
            await _interactionService.RegisterCommandsToGuildAsync(_options.GuildId.Value, true);
        }
        else
        {
            var commands = await _interactionService.RegisterCommandsGloballyAsync(true);

            LogCommands(commands);
        }
    }

    private void LogCommands(IEnumerable<RestGlobalCommand> commands)
    {
        var commandList = commands
            .SelectMany(command =>
            {
                var entry = $"/{command.Name}";

                var entries = new List<string>();
                if (command.Options.Count == 0 || !command.Options.All(o => o.Type == ApplicationCommandOptionType.SubCommand || o.Type == ApplicationCommandOptionType.SubCommandGroup))
                {
                    entries.Add(entry);
                }

                entries.AddRange(command.Options
                    .Where(option => option.Type == ApplicationCommandOptionType.SubCommand || option.Type == ApplicationCommandOptionType.SubCommandGroup)
                    .Select(option => $"{entry} {option.Name}"));

                return entries;
            })
            .ToList();

        Logger.LogInformation($"Added commands: {string.Join(", ", commandList)}");
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
