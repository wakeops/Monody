namespace Monody.Services.Weather.Utils;

internal static class WindBearingConverter
{
    public static string ConvertToWindDirection(int? bearing)
    {
        if (bearing == null)
        {
            return "";
        }

        return (int)(bearing / 11.25) switch
        {
            0 or 31 => "N",
            1 or 2 => "NNE",
            3 or 4 => "NE",
            5 or 6 => "ENE",
            7 or 8 => "E",
            9 or 10 => "ESE",
            11 or 12 => "SE",
            13 or 14 => "SSE",
            15 or 16 => "S",
            17 or 18 => "SSW",
            19 or 20 => "SW",
            21 or 22 => "WSW",
            23 or 24 => "W",
            25 or 26 => "WNW",
            27 or 28 => "NW",
            29 or 30 => "NNW",
            _ => "",
        };
    }
}
