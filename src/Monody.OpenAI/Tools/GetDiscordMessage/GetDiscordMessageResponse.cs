using System;
using System.Collections.Generic;

namespace Monody.OpenAI.Tools.GetDiscordMessage;

internal sealed class GetDiscordMessageResponse
{
    public string GuildName { get; set; }
    public ulong? GuildId { get; set; }

    public string ChannelName { get; set; }
    public ulong ChannelId { get; set; }
    public string ChannelType { get; set; }

    public ulong MessageId { get; set; }
    public string Content { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public ulong AuthorId { get; set; }
    public string AuthorUsername { get; set; }
    public string AuthorGlobalName { get; set; }

    public IReadOnlyList<string> Attachments { get; set; } = [];
    public IReadOnlyList<string> Embeds { get; set; } = [];
}
