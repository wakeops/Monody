using System;

namespace Monody.Data.Entities;

/// <summary>
/// A /slop conversation, keyed by the interaction that started it so follow-ups can find it.
/// </summary>
/// <remarks>
/// Persisted rather than held in memory: a redeploy or a crash would otherwise drop every
/// conversation in flight, and the user just sees "I lost this conversation's context".
/// </remarks>
public class Conversation
{
    /// <summary>The id of the interaction that started the thread. Not generated.</summary>
    public ulong Id { get; set; }

    public ulong UserId { get; set; }

    public ulong? ChannelId { get; set; }

    public ulong? GuildId { get; set; }

    /// <summary>The turns, as a JSON array of {role, content}. Whole-row read and write.</summary>
    public string TurnsJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>One stored turn. Tool calls and their results are deliberately not kept.</summary>
public sealed record ConversationTurn(string Role, string Content);
