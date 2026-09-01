using System.ComponentModel;

namespace Monody.AI.Tools.Capabilities.CurrentTime;

public sealed class CurrentTimeToolRequest
{
    [Description(
        "A time zone or a place. An IANA id such as 'Europe/London' works, and so does any place " +
        "name - 'Raleigh, NC', 'Tokyo', 'the Isle of Skye' - which is looked up. Leave empty for UTC.")]
    public string TimeZone { get; set; }
}
