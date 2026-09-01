using Discord;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Monody.Bot.Modules.Slop.Utils;

public static class DiscordHelper
{
    public static void EnrichWithInteractionContext(ChatHistory history, ulong interactionId, IGuild guild, IChannel channel)
    {
        const string NoAccess = "unknown, you may not have sufficient permissions to access this data.";

        history.AddUserMessage(string.Join('\n',
            "[Context: data related to the initiating discord interaction.]",
            $"Discord Interaction: Id = '{interactionId}'",
            guild != null
                ? $"Discord Guild: Id = '{guild.Id}', Name = '{guild.Name}'"
                : $"Discord Guild: {NoAccess}",
            channel != null
                ? $"Discord Channel: Id = '{channel.Id}', Name = '{channel.Name}', Type = '{channel.GetChannelType()}'"
                : $"Discord Channel: {NoAccess}"));
    }
}
