using System;

namespace Monody.Data.Entities;

/// <summary>
/// A durable personal fact about a Discord user. Deliberately narrow: this is not a general
/// note store, and the categories below are the whole of what may be kept.
/// </summary>
public class UserMemory
{
    public int Id { get; set; }

    public ulong UserId { get; set; }

    public MemoryCategory Category { get; set; }

    public string Content { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public enum MemoryCategory
{
    /// <summary>What the user wants to be called. One per user.</summary>
    Name,

    /// <summary>Where the user lives, for weather and local time. One per user.</summary>
    Location,

    /// <summary>The user's time zone, as an IANA id. One per user.</summary>
    TimeZone,

    /// <summary>A lasting preference about how the user wants to be helped. Several allowed.</summary>
    Preference
}
