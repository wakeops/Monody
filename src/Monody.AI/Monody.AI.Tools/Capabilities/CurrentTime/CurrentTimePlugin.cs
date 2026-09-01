using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using TimeZoneNames;

namespace Monody.AI.Tools.Capabilities.CurrentTime;

/// <summary>
/// The model has no idea what "now" is, so questions like "what time is it in London" and even
/// "what is today's date" are unanswerable without this.
/// </summary>
public sealed class CurrentTimePlugin
{
    private readonly TimeProvider _timeProvider;

    public CurrentTimePlugin() : this(TimeProvider.System)
    {
    }

    public CurrentTimePlugin(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    [KernelFunction("current_time")]
    [Description("Returns the current date and time, optionally for a given time zone or city. Always use this instead of guessing what the current date or time is.")]
    public Task<CurrentTimeToolResponse> GetCurrentTimeAsync(CurrentTimeToolRequest request, CancellationToken cancellationToken = default)
    {
        var zone = ResolveTimeZone(request?.TimeZone);

        var utcNow = _timeProvider.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(utcNow, zone);
        var abbreviation = GetAbbreviation(zone, localNow);

        var description = localNow.ToString("dddd, dd MMMM yyyy HH:mm", CultureInfo.InvariantCulture);

        return Task.FromResult(new CurrentTimeToolResponse
        {
            TimeZone = zone.Id,
            LocalTime = localNow.ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture),
            LocalTimeDescription = string.IsNullOrEmpty(abbreviation) ? description : $"{description} {abbreviation}",
            UtcTime = utcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            UtcOffset = localNow.Offset.ToString(localNow.Offset < TimeSpan.Zero ? "'-'hh':'mm" : "'+'hh':'mm", CultureInfo.InvariantCulture),
            Abbreviation = abbreviation,
            IsDaylightSavingTime = zone.IsDaylightSavingTime(utcNow),
            DiscordTimestamp = $"<t:{utcNow.ToUnixTimeSeconds()}:F>"
        });
    }

    /// <summary>
    /// Accepts an IANA id, a Windows id, or - because the model does not always supply a proper
    /// id - a bare city name like "London".
    /// </summary>
    private static TimeZoneInfo ResolveTimeZone(string timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
        {
            return TimeZoneInfo.Utc;
        }

        var requested = timeZone.Trim();

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(requested);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return FindByCityName(requested)
                   ?? throw new ArgumentException(
                       $"'{requested}' is not a recognised time zone. Supply an IANA id such as 'Europe/London'.",
                       nameof(timeZone),
                       ex);
        }
    }

    /// <summary>Matches the city segment of an IANA id, so "new york" finds "America/New_York".</summary>
    private static TimeZoneInfo FindByCityName(string city)
    {
        var normalized = city.Replace(' ', '_');

        return TimeZoneInfo.GetSystemTimeZones()
            .Where(zone => zone.Id.Contains('/'))
            // Ordered so a given city name always resolves to the same zone.
            .OrderBy(zone => zone.Id, StringComparer.Ordinal)
            .FirstOrDefault(zone =>
                zone.Id[(zone.Id.LastIndexOf('/') + 1)..].Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetAbbreviation(TimeZoneInfo zone, DateTimeOffset localNow)
    {
        try
        {
            var abbreviations = TZNames.GetAbbreviationsForTimeZone(zone.Id, "en-US");

            if (abbreviations is null)
            {
                return string.Empty;
            }

            var seasonal = zone.IsDaylightSavingTime(localNow)
                ? abbreviations.Daylight
                : abbreviations.Standard;

            return seasonal ?? abbreviations.Generic ?? string.Empty;
        }
        catch (Exception)
        {
            // TimeZoneNames only knows IANA ids; a Windows id or an unmapped zone just goes unlabelled.
            return string.Empty;
        }
    }
}
