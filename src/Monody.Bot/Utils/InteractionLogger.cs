using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Monody.Bot.Utils;

internal class InteractionLogger
{
    private readonly ILogger _logger;

    public InteractionLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void LogInteraction(SocketInteraction interaction)
    {
        try
        {
            switch (interaction)
            {
                case SocketSlashCommand slash:
                    LogSlashCommand(slash);
                    break;

                case SocketUserCommand userCmd:
                    LogUserCommand(userCmd);
                    break;

                case SocketMessageCommand msgCmd:
                    LogMessageCommand(msgCmd);
                    break;

                case SocketMessageComponent component:
                    LogComponentInteraction(component);
                    break;

                default:
                    throw new NotImplementedException($"Unkonwn Interaction: {interaction.GetType().Name}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log interaction");
        }
    }

    private void LogSlashCommand(SocketSlashCommand cmd)
    {
        var fullPath = BuildSlashCommandName(cmd.Data);
        var args = BuildSlashCommandArgumentsObject(cmd.Data.Options);

        _logger.LogInformation("Slash Command: {Command} | Invoker: {@User} | Args: {Args}",
            fullPath,
            new { cmd.User.Id, cmd.User.Username },
            args);
    }
    private void LogUserCommand(SocketUserCommand cmd)
    {
        using (LogContext.PushProperty("TargetUser", new { cmd.Data.Member.Id, cmd.Data.Member.Username }))
        {
            _logger.LogInformation("User Command: /{Command} | Invoker: {@User} | Target: {@TargetUser}",
                cmd.CommandName,
                new { cmd.User.Id, cmd.User.Username },
                new { cmd.Data.Member.Id, cmd.Data.Member.Username });
        }
    }

    private void LogMessageCommand(SocketMessageCommand cmd)
    {
        _logger.LogInformation("Message Command: /{Command} | Invoker: {@User} | Target Message: {MessageId}",
            cmd.CommandName,
            new { cmd.User.Id, cmd.User.Username },
            cmd.Data.Message.Id);
    }

    private void LogComponentInteraction(SocketMessageComponent component)
    {
        _logger.LogInformation("Component Interaction: {Id} | Invoker: {@User} | Value: {Value}",
            component.Data.CustomId,
            new { component.User.Id, component.User.Username },
            string.Join(", ", component.Data.Values ?? []));
    }

    private static string BuildSlashCommandName(SocketSlashCommandData data)
    {
        string name = $"/{data.Name}";
        var current = data.Options.FirstOrDefault();

        // Walk down subcommand/subcommand-group structure
        while (current is { Type: ApplicationCommandOptionType.SubCommand or ApplicationCommandOptionType.SubCommandGroup })
        {
            name += $" {current.Name}";
            current = current.Options.FirstOrDefault();
        }

        return name;
    }

    private static Dictionary<string, object> BuildSlashCommandArgumentsObject(IEnumerable<SocketSlashCommandDataOption> options)
    {
        var flat = FlattenOptions(options).ToList();

        var dict = new Dictionary<string, object>();

        foreach (var opt in flat)
        {
            dict[opt.Name] = ConvertOptionValue(opt.Value);
        }

        return dict;
    }

    private static IEnumerable<SocketSlashCommandDataOption> FlattenOptions(IEnumerable<SocketSlashCommandDataOption> opts)
    {
        foreach (var opt in opts)
        {
            if (opt.Type is ApplicationCommandOptionType.SubCommand or ApplicationCommandOptionType.SubCommandGroup)
            {
                foreach (var nested in FlattenOptions(opt.Options))
                {
                    yield return nested;
                }
            }
            else
            {
                yield return opt;
            }
        }
    }

    private static object ConvertOptionValue(object value)
    {
        return value switch
        {
            null => null,
            SocketUser user => new { user.Id, user.Username },
            SocketChannel ch => new { ch.Id, ChannelType = ch.GetType().Name },
            SocketRole role => new { role.Id, role.Name },
            _ => value
        };
    }
}
