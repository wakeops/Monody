using System.Collections.Generic;
using DarkSky.Models;

namespace Monody.Module.Weather.Utils;

internal static class EmojiIconMap
{
    private static readonly Dictionary<Icon, string> _emojiMap = new ()
    {
        { Icon.ClearDay, ":sunny:" },
        { Icon.ClearNight, ":sunny:" },
        { Icon.Rain, ":cloud_rain:" },
        { Icon.Snow, ":cloud_snow:" },
        { Icon.Sleet, ":cloud_rain:" },
        { Icon.Wind, ":wind_blowing_face:" },
        { Icon.Fog, ":foggy:" },
        { Icon.Cloudy, ":cloud:" },
        { Icon.PartlyCloudyDay, ":white_sun_cloud:" },
        { Icon.PartlyCloudyNight, ":white_sun_cloud:" }
    };

    public static string Resolve(Icon icon)
    {
        return _emojiMap.GetValueOrDefault(icon);
    }
}
