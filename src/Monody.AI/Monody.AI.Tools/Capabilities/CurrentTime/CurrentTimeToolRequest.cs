using System.ComponentModel;

namespace Monody.AI.Tools.Capabilities.CurrentTime;

public sealed class CurrentTimeToolRequest
{
    [Description(
        "IANA time zone id such as 'Europe/London' or 'America/New_York'. A bare city name like " +
        "'London' is also accepted. Leave empty for UTC.")]
    public string TimeZone { get; set; }
}
