namespace Monody.Bot.Modules.Weather;

internal static class Constants
{
    public const string FooterPoweredByText = "Powered by Pirate Weather";
    public const string TitleSeeMoreText = "See more at merrysky.com";
    public const string TitleSeeMoreUrlFormat = "https://merrysky.net/forecast/{0},{1}";
    public const int MaxForecastDays = 5;
    public const int MaxForecastHours = 24;
    public const int ForecastHoursPerPageLimit = 4;
    public const uint DefaultEmbedColor = 0x6DC8C2;
    public const int MaxLocationNameLength = 100;
}
