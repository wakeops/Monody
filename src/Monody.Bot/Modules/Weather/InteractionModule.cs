using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DarkSky.Models;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using TimeZoneNames;
using Monody.Services.Geocode;
using Monody.Services.Weather;
using Monody.Services.Geocode.Models;
using Monody.Services.Weather.Models;
using Monody.Bot.Modules.Weather.Utils;

namespace Monody.Bot.Modules.Weather;

[Group("weather", "Weather commands")]
public class InteractionModule : InteractionModuleBase<SocketInteractionContext>
{
    private const string ForecastUnavailable = "Failed to find a forecast for this location.";
    private const string LocationUnresolved = "Failed to resolve this location.";

    private readonly GeocodeService _geocodeService;
    private readonly WeatherService _weatherService;

    public InteractionModule(GeocodeService geocodeService, WeatherService weatherService)
    {
        _geocodeService = geocodeService;
        _weatherService = weatherService;
    }

    [SlashCommand("now", "Get the current forecast.")]
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.BotDm, InteractionContextType.Guild)]
    public async Task GetWeatherNowAsync(
        [Summary("Location", "Where would like to forecast?")]
        [MaxLength(Constants.MaxLocationNameLength)]
        string location,
        [Summary("Units", "Units type")]
        MeasurementUnits? paramUnits = null)
    {
        await DeferAsync();

        var request = await ResolveRequestAsync(location, paramUnits);
        if (request is null)
        {
            return;
        }

        var (weatherLocation, unit) = request.Value;

        var forecastData = await _weatherService.GetCurrentForecastAsync(
            weatherLocation.Coordinates.Latitude, weatherLocation.Coordinates.Longitude, unit);

        if (forecastData == null)
        {
            await SetContentAsync(ForecastUnavailable);
            return;
        }

        var forecast = forecastData.Data;

        var embed = BuildEmbed(
            weatherLocation,
            BuildCurrentFields(forecast, unit),
            BuildCurrentDescription(forecast, unit, forecastData.TimeZone));

        await ModifyOriginalResponseAsync(properties => properties.Embed = embed);
    }

    [SlashCommand("hourly", "Get the hourly forecast.")]
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.BotDm, InteractionContextType.Guild)]
    public Task GetWeatherHourlyAsync(
        [Summary("Location", "Where would like to forecast?")]
        [MaxLength(Constants.MaxLocationNameLength)]
        string location,
        [Summary("Units", "Units type")]
        MeasurementUnits? paramUnits = null)
        => ProcessGetWeatherHourlyAsync(0, location, paramUnits);

    [ComponentInteraction("forecast_hourly_*_(*)_*", true)]
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.BotDm, InteractionContextType.Guild)]
    public async Task GetWeatherHourly_ButtonAsync(int page, string encodedLocation, MeasurementUnits? unit)
    {
        // Only the user who ran the original command may page through it.
        if (Context.Interaction is not SocketMessageComponent component ||
            component.Message.Interaction.User.Id != Context.Interaction.User.Id)
        {
            await RespondAsync();
            return;
        }

        var location = string.IsNullOrEmpty(encodedLocation) ? null : Uri.UnescapeDataString(encodedLocation);

        await ProcessGetWeatherHourlyAsync(page, location, unit);
    }

    private async Task ProcessGetWeatherHourlyAsync(int page, string location, MeasurementUnits? paramUnits)
    {
        await DeferAsync();

        var request = await ResolveRequestAsync(location, paramUnits);
        if (request is null)
        {
            return;
        }

        var (weatherLocation, unit) = request.Value;

        var forecastData = await _weatherService.GetHourlyForecastAsync(
            weatherLocation.Coordinates.Latitude, weatherLocation.Coordinates.Longitude, unit);

        if (forecastData == null)
        {
            await SetContentAsync(ForecastUnavailable);
            return;
        }

        var fields = forecastData.Data
            .Skip(page * Constants.ForecastHoursPerPageLimit)
            .Take(Constants.ForecastHoursPerPageLimit)
            .Select(hour => BuildHourField(hour, unit, forecastData.TimeZone));

        var embed = BuildEmbed(weatherLocation, fields);
        var components = BuildHourlyPager(page, location, unit);

        await ModifyOriginalResponseAsync(properties =>
        {
            properties.Embed = embed;
            properties.Components = components;
        });
    }

    [SlashCommand("week", "Get the weekly forecast.")]
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.BotDm, InteractionContextType.Guild)]
    public async Task GetWeatherWeekAsync(
        [Summary("Location", "Where would like to forecast?")]
        [MaxLength(Constants.MaxLocationNameLength)]
        string location,
        [Summary("Units", "Units type")]
        MeasurementUnits? paramUnits = null)
    {
        await DeferAsync();

        var request = await ResolveRequestAsync(location, paramUnits);
        if (request is null)
        {
            return;
        }

        var (weatherLocation, unit) = request.Value;

        var forecastData = await _weatherService.GetDailyForecastAsync(
            weatherLocation.Coordinates.Latitude, weatherLocation.Coordinates.Longitude, Constants.MaxForecastDays, unit);

        if (forecastData == null)
        {
            await SetContentAsync(ForecastUnavailable);
            return;
        }

        var fields = forecastData.Data.Select(day => new EmbedFieldBuilder()
            .WithIsInline(false)
            .WithName(day.Date.ToString("dddd MMMM d"))
            .WithValue($"{EmojiIconMap.Resolve(day.Icon)} {FormatTemp(day.High, unit)} / {FormatTemp(day.Low, unit)} - {day.Summary}"));

        var embed = BuildEmbed(weatherLocation, fields);

        await ModifyOriginalResponseAsync(properties => properties.Embed = embed);
    }

    /// <summary>
    /// Geocodes the location and settles on a unit system. Returns null after replying with an
    /// error when the location can't be resolved.
    /// </summary>
    private async Task<(LocationDetails Location, MeasurementUnits Unit)?> ResolveRequestAsync(string location, MeasurementUnits? paramUnits)
    {
        var weatherLocation = await _geocodeService.GetGeocodeForLocationStringAsync(location);

        if (weatherLocation?.Coordinates == null)
        {
            await SetContentAsync(LocationUnresolved);
            return null;
        }

        return (weatherLocation, paramUnits ?? GuessMeasurementUnit(weatherLocation));
    }

    private Task SetContentAsync(string content) =>
        ModifyOriginalResponseAsync(properties => properties.Content = content);

    private static string BuildCurrentDescription(ForecastNow forecast, MeasurementUnits unit, string timeZone)
    {
        var description = new StringBuilder();

        description.Append(
            $"{EmojiIconMap.Resolve(forecast.Icon)} Currently {FormatTemp(forecast.Temperature, unit)} and {forecast.Condition} " +
            $"with a high of {FormatTemp(forecast.ForecastHigh, unit)} and a low of {FormatTemp(forecast.ForecastLow, unit)}.");

        var alerts = forecast.Alerts?.ToList() ?? [];
        if (alerts.Count == 0)
        {
            return description.ToString();
        }

        description.AppendLine();

        var timeZoneCode = GetTimeZoneCode(timeZone);

        foreach (var alert in alerts)
        {
            // Alert titles read "<name> issued <date> by <office>"; keep only the name.
            var issuedIndex = alert.Title.IndexOf("issued", StringComparison.Ordinal);
            var title = issuedIndex > 0 ? alert.Title[..issuedIndex].Trim() : alert.Title;

            description.AppendLine($"[**{title}**]({alert.Uri}) Until {alert.ExpirationDate:dd MMM yy HH:mm} {timeZoneCode}");
        }

        return description.ToString();
    }

    private static IEnumerable<EmbedFieldBuilder> BuildCurrentFields(ForecastNow forecast, MeasurementUnits unit)
    {
        var accumulation = GetPrecipitationAccumulation(forecast);
        if (accumulation > 0.1)
        {
            yield return Field("Precipitation",
                $"There is a {forecast.PrecipitationProbability:P0} chance of {forecast.PrecipitationType.ToString().ToLower()} " +
                $"with an estimated accumulation of {accumulation:F1} inches");
        }

        yield return Field("Wind", $"{forecast.WindSpeed:F1} MpH with gusts up to {forecast.WindGust:F1} MpH");
        yield return Field("Humidity", $"{forecast.Humidity:N0}%");

        if (forecast.Temperature >= 80 && forecast.Humidity >= 40)
        {
            yield return Field("Heat Index", FormatTemp(forecast.HeatIndex, unit));
        }

        if (forecast.Temperature <= 50 && forecast.WindGust >= 3)
        {
            yield return Field("Wind Chill", FormatTemp(forecast.WindChill, unit));
        }

        if (forecast.UVIndex > 0)
        {
            yield return Field("UV Index", $"({forecast.UVIndex}) {GetUvIndexDescription(forecast.UVIndex)}");
        }

        static EmbedFieldBuilder Field(string name, string value) =>
            new EmbedFieldBuilder().WithIsInline(true).WithName(name).WithValue(value);
    }

    /// <summary>Estimated 24h accumulation in inches, or 0 when precipitation is unlikely.</summary>
    private static double GetPrecipitationAccumulation(ForecastNow forecast)
    {
        if (forecast.PrecipitationProbability < 0.05)
        {
            return 0;
        }

        if (forecast.SnowAccumulation > 0 && forecast.PrecipitationType == PrecipitationType.Snow)
        {
            return forecast.SnowAccumulation;
        }

        return forecast.PrecipitationIntensity > 0 ? forecast.PrecipitationIntensity * 24 : 0;
    }

    private static EmbedFieldBuilder BuildHourField(ForecastHour hour, MeasurementUnits unit, string timeZone)
    {
        var localTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(hour.Date, timeZone);

        return new EmbedFieldBuilder()
            .WithIsInline(false)
            .WithName($"{localTime:h:mm tt} - {EmojiIconMap.Resolve(hour.Icon)} {hour.Summary}")
            .WithValue(
                $"{FormatTemp(hour.Temperature, unit)} | :droplet: {hour.PrecipitationProbability:N0}% " +
                $"({hour.PrecipitationIntensity:F2} in) | :dash: {hour.WindSpeed:N0} mph {hour.CardinalWindBearing}");
    }

    private static MessageComponent BuildHourlyPager(int page, string location, MeasurementUnits unit)
    {
        var encodedLocation = string.IsNullOrEmpty(location) ? null : Uri.EscapeDataString(location);
        var lastPage = Constants.MaxForecastHours / Constants.ForecastHoursPerPageLimit - 1;

        return new ComponentBuilder()
            .WithButton(
                customId: $"forecast_hourly_{page - 1}_({encodedLocation})_{unit}",
                emote: new Emoji("⬅️"),
                disabled: page == 0)
            .WithButton(
                customId: $"forecast_hourly_{page + 1}_({encodedLocation})_{unit}",
                emote: new Emoji("➡️"),
                disabled: page >= lastPage)
            .Build();
    }

    private static Embed BuildEmbed(LocationDetails location, IEnumerable<EmbedFieldBuilder> fields, string description = null)
    {
        var embed = new EmbedBuilder()
            .WithAuthor(FormatLocation(location))
            .WithTitle(Constants.TitleSeeMoreText)
            .WithUrl(string.Format(Constants.TitleSeeMoreUrlFormat, location.Coordinates.Latitude, location.Coordinates.Longitude))
            .WithColor(new Color(MonodyConstants.DefaultEmbedColor))
            .WithFooter(Constants.FooterPoweredByText);

        if (!string.IsNullOrEmpty(description))
        {
            embed.WithDescription(description);
        }

        if (fields != null)
        {
            embed.WithFields(fields);
        }

        return embed.Build();
    }

    private static string GetUvIndexDescription(int uvIndex) => uvIndex switch
    {
        < 3 => "Low",
        < 6 => "Moderate",
        < 8 => "High",
        < 11 => "Very High",
        _ => "Extreme"
    };

    private static MeasurementUnits GuessMeasurementUnit(LocationDetails location) =>
        location.Country is "United States" or "USA" ? MeasurementUnits.Imperial : MeasurementUnits.Metric;

    private static string FormatTemp(double temperature, MeasurementUnits unit) =>
        unit == MeasurementUnits.Imperial ? $"{temperature:N0} °F" : $"{temperature:N0} °C";

    private static string FormatLocation(LocationDetails location)
    {
        var prefix = new StringBuilder();

        if (!string.IsNullOrEmpty(location.City))
        {
            prefix.Append($"{location.City}, ");
        }

        if (!string.IsNullOrEmpty(location.Region))
        {
            prefix.Append($"{location.Region} - ");
        }

        return prefix.Append(location.Country).ToString();
    }

    private static string GetTimeZoneCode(string timezone)
    {
        try
        {
            return TZNames.GetAbbreviationsForTimeZone(timezone, "en-US")?.Generic;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
