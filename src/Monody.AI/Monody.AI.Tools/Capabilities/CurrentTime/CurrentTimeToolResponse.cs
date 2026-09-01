using System.ComponentModel;

namespace Monody.AI.Tools.Capabilities.CurrentTime;

public sealed class CurrentTimeToolResponse
{
    [Description("The time zone this result is for, as an IANA id.")]
    public string TimeZone { get; set; }

    [Description("Current local date and time in that zone, ISO 8601.")]
    public string LocalTime { get; set; }

    [Description("Current local date and time written out for a human, e.g. 'Monday, 01 September 2026 14:32 BST'.")]
    public string LocalTimeDescription { get; set; }

    [Description("Current UTC date and time, ISO 8601.")]
    public string UtcTime { get; set; }

    [Description("Offset from UTC, e.g. '+01:00'.")]
    public string UtcOffset { get; set; }

    [Description("Time zone abbreviation currently in effect, e.g. 'BST'. May be empty.")]
    public string Abbreviation { get; set; }

    [Description("True when daylight saving time is currently in effect in that zone.")]
    public bool IsDaylightSavingTime { get; set; }

    [Description(
        "Discord timestamp markup. Include this verbatim in a reply to render the moment in each " +
        "reader's own local time and format.")]
    public string DiscordTimestamp { get; set; }
}
