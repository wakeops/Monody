using System.ComponentModel;

namespace Monody.AI.Tools.Capabilities.CurrentTime;

public sealed class CurrentTimeToolRequest
{
    [Description(
        "A time zone identifier, such as 'Europe/London' or 'America/New_York'. Not a city or " +
        "region name - convert those to their zone yourself. Leave empty for UTC.")]
    public string TimeZone { get; set; }
}
