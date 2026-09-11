using Discord;
using Discord.WebSocket;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Monody.App.Modules.Slop.Utils;

public static class DiscordHelper
{
    private const string _noAccess = "unknown, you may not have sufficient permissions to access this data.";

    public static void EnrichWithInteractionContext(ChatHistory history, ulong interactionId, SocketInteraction interactionContext)
    {
        var guildId = interactionContext.GuildId;
        var channel = interactionContext.Channel;

        history.AddUserMessage(string.Join('\n',
            "[Context: data related to the initiating discord interaction.]",
            $"Discord Interaction: Id = '{interactionId}'",
            guildId != 0
                ? $"Discord Guild: Id = '{guildId}'"
                : $"Discord Guild: {_noAccess}",
            channel != null
                ? $"Discord Channel: Id = '{channel.Id}', Name = '{channel.Name}', Type = '{channel.GetChannelType()}'"
                : $"Discord Channel: {_noAccess}"));
    }
}
