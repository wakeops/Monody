using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting.Util;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monody.Bot.ModuleBuilder.Models;
using Monody.Bot.Options;

namespace Monody.Bot.Services;

internal partial class ModuleLoaderService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordOptions _options;
    private readonly ModuleLoaderConfig _moduleLoaderConfig;
    private readonly ILogger<ModuleLoaderService> _logger;

    public ModuleLoaderService(DiscordSocketClient client, InteractionService interactionService, IServiceProvider serviceProvider,
        IOptions<DiscordOptions> options, IOptions<ModuleLoaderConfig> moduleLoaderConfig, ILogger<ModuleLoaderService> logger)
    {
        _client = client;
        _interactionService = interactionService;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _moduleLoaderConfig = moduleLoaderConfig.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var moduleAssemblies = _moduleLoaderConfig.ModuleConfigs.Select(x => x.Assembly).Distinct();

        foreach (var assembly in moduleAssemblies)
        {
            await _interactionService.AddModulesAsync(assembly, _serviceProvider);
        }

        await _client.WaitForReadyAsync(cancellationToken);

        
        IReadOnlyCollection<RestApplicationCommand> commands;
        if (_options.GuildId != null)
        {
            commands = await _interactionService.RegisterCommandsToGuildAsync(_options.GuildId.Value, true);
        }
        else
        {
            commands = await _interactionService.RegisterCommandsGloballyAsync(true);
        }

        LogCommands(commands);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void LogCommands(IEnumerable<RestApplicationCommand> commands)
    {
        var commandList = commands
            .SelectMany(command => GetCommandSignatures($"/{command.Name}", command.Options))
            .ToList();

        _logger.LogInformation("Registered Commands: {Commands}", string.Join(", ", commandList));
    }

    private static IEnumerable<string> GetCommandSignatures(string basePath, IEnumerable<IApplicationCommandOption> options)
    {
        // Only consider subcommands and subcommand groups
        var subCommandOptions = options
            .Where(o => o.Type == ApplicationCommandOptionType.SubCommand ||
                        o.Type == ApplicationCommandOptionType.SubCommandGroup)
            .ToList();

        // If there are no required subcommands, this path is a valid command
        if (!subCommandOptions.Any(o => o.IsRequired.GetValueOrDefault(true)))
        {
            yield return basePath;
        }

        // Recurse into each subcommand, extending the base path
        foreach (var option in subCommandOptions)
        {
            var nextPath = $"{basePath} {option.Name}";

            foreach (var signature in GetCommandSignatures(nextPath, option.Options))
            {
                yield return signature;
            }
        }
    }
}
