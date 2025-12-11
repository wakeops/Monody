using System;
using System.Collections.Generic;

namespace Monody.AI.Tools.Capabilities.GetDiscordMessageHistory;

internal sealed class GetDiscordMessageHistoryResponse
{
    public ulong? GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string ChannelType { get; set; }

    public List<GetDiscordMessageHistoryMessage> Messages = [];
}

internal sealed class GetDiscordMessageHistoryMessage
{
    public ulong MessageId { get; set; }
    public string Content { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public ulong AuthorId { get; set; }
    public string AuthorUsername { get; set; }
    public string AuthorGlobalName { get; set; }

    public IReadOnlyList<string> Attachments { get; set; } = [];
    public IReadOnlyList<string> Embeds { get; set; } = [];
}