using System;

namespace Monody.Data.Entities;

public class Reminder
{
    public int Id { get; set; }

    public ulong UserId { get; set; }

    /// <summary>Where to deliver it. Null when the reminder was set somewhere unreachable.</summary>
    public ulong? ChannelId { get; set; }

    public string Message { get; set; }

    public DateTimeOffset DueAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? DeliveredAt { get; set; }
}
