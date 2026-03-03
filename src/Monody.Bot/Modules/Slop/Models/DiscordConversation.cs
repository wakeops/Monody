using Microsoft.SemanticKernel.ChatCompletion;

namespace Monody.Bot.Modules.Slop.Models;

public class DiscordConversation
{
    public string Id { get; set; }

    public ulong? GuildId { get; set; }

    public ulong? ChannelId { get; set; }

    public ulong InitialUserId { get; set; }

    public ChatHistory History { get; set; }

    public DiscordConversation(string conversationId, ulong? guildId, ulong? channelId, ulong userId, ChatHistory history)
    {
        Id = conversationId;
        GuildId = guildId;
        ChannelId = channelId;
        InitialUserId = userId;
        History = history;
    }
}
