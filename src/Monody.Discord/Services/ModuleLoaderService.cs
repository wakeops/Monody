using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Monody.Discord.Services;

internal class ModuleLoaderService : DiscordClientService
{
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordOptions _options;
    private readonly InteractionModuleOptions _moduleOptions;

    public ModuleLoaderService(DiscordSocketClient client, InteractionService interactionService, IServiceProvider serviceProvider,
        IOptions<DiscordOptions> options, IOptions<InteractionModuleOptions> moduleOptions, ILogger<ModuleLoaderService> logger)
        : base(client, logger)
    {
        _interactionService = interactionService;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _moduleOptions = moduleOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        foreach (var assembly in _moduleOptions.Assemblies.Distinct())
        {
            await _interactionService.AddModulesAsync(assembly, _serviceProvider);
        }

        await Client.WaitForReadyAsync(cancellationToken);

        // Registration replaces the whole command set, so registering nothing would delete every
        // existing command from Discord.
        if (_interactionService.Modules.Count == 0)
        {
            Logger.LogError("No interaction modules were found; skipping command registration so existing commands are kept");
            return;
        }

        // A guild id registers commands to that one guild, which propagates instantly and is
        // the usual choice while developing; otherwise they go out globally.
        IReadOnlyCollection<RestApplicationCommand> commands = _options.GuildId is ulong guildId
            ? await _interactionService.RegisterCommandsToGuildAsync(guildId, true)
            : await _interactionService.RegisterCommandsGloballyAsync(true);

        var signatures = commands.SelectMany(command => GetCommandSignatures($"/{command.Name}", command.Options));

        Logger.LogInformation("Registered Commands: {Commands}", string.Join(", ", signatures));
    }

    private static IEnumerable<string> GetCommandSignatures(string basePath, IEnumerable<IApplicationCommandOption> options)
    {
        var subCommands = options
            .Where(o => o.Type is ApplicationCommandOptionType.SubCommand or ApplicationCommandOptionType.SubCommandGroup)
            .ToList();

        // A command that groups subcommands isn't invocable on its own; only leaves are.
        if (subCommands.Count == 0)
        {
            yield return basePath;
            yield break;
        }

        foreach (var subCommand in subCommands)
        {
            foreach (var signature in GetCommandSignatures($"{basePath} {subCommand.Name}", subCommand.Options))
            {
                yield return signature;
            }
        }
    }
}
