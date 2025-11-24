using System.Collections.Generic;
using OpenAI.Chat;

namespace Monody.Module.AIChat.Models;

public class DiscordConversation
{
    public string Id { get; set; }

    public ulong? GuildId { get; set; }

    public ulong? ChannelId { get; set; }

    public ulong InitialUserId { get; set; }

    public List<ChatMessage> Messages { get; set; }

    public DiscordConversation(string conversationId, ulong? guildId, ulong? channelId, ulong userId, List<ChatMessage> messages)
    {
        Id = conversationId;
        GuildId = guildId;
        ChannelId = channelId;
        InitialUserId = userId;
        Messages = messages;
    }
}
