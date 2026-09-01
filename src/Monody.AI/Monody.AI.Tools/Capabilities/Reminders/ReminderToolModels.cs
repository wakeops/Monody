using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Monody.Data;

namespace Monody.AI.Tools.Capabilities.Reminders;

public sealed class SetReminderToolRequest
{
    [Description("What to remind the user about, in their own words where possible.")]
    [Required]
    [MaxLength(DataConstants.MaxReminderLength)]
    public string Message { get; set; }

    [Description(
        "How many minutes from now to fire. Use this for relative requests like 'in 2 hours'. " +
        "Set to 0 when using DueAtUtc instead.")]
    [Range(0, 525600)]
    public int DelayMinutes { get; set; }

    [Description(
        "Absolute UTC time to fire, ISO 8601, e.g. '2026-09-02T14:30:00Z'. Use this only for a " +
        "specific clock time, and call current_time first so you know what 'today' is. " +
        "Empty string when using DelayMinutes.")]
    public string DueAtUtc { get; set; }
}

public sealed class SetReminderToolResponse
{
    [Description("Whether the reminder was scheduled.")]
    public bool Scheduled { get; set; }

    [Description("When it will fire, as Discord timestamp markup. Include it verbatim in your reply.")]
    public string DueAt { get; set; }

    [Description("Why it was rejected, when Scheduled is false.")]
    public string Outcome { get; set; }
}

public sealed class ListRemindersToolRequest
{
    [Description("Unused. Always lists the current user's pending reminders.")]
    public string Unused { get; set; }
}

public sealed class ListRemindersToolResponse
{
    [Description("Pending reminders, soonest first. Each entry is 'due — message'.")]
    public System.Collections.Generic.List<string> Reminders { get; set; } = [];
}
