using System.Collections.Generic;
using Monody.AI.Domain.Models;

namespace Monody.Module.AIChat.Models;

public class DiscordConversation
{
    public string Id { get; set; }

    public ulong? GuildId { get; set; }

    public ulong? ChannelId { get; set; }

    public ulong InitialUserId { get; set; }

    public List<ChatMessageDto> Messages { get; set; }

    public DiscordConversation(string conversationId, ulong? guildId, ulong? channelId, ulong userId, List<ChatMessageDto> messages)
    {
        Id = conversationId;
        GuildId = guildId;
        ChannelId = channelId;
        InitialUserId = userId;
        Messages = messages;
    }
}
