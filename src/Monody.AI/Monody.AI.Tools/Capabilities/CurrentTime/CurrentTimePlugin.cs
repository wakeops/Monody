using System;
using System.ComponentModel;
using System.Globalization;
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

    // Exactly one constructor: Semantic Kernel activates plugins through ActivatorUtilities,
    // which throws when more than one overload can be satisfied from the container.
    public CurrentTimePlugin(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    [KernelFunction("current_time")]
    [Description(
        "Returns the current date and time for a time zone. Always use this instead of guessing " +
        "what the current date or time is. TimeZone must be a time zone identifier, not a place: " +
        "if the user names a city, work out its zone yourself and pass that, e.g. 'Raleigh, NC' " +
        "becomes 'America/New_York'.")]
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
    /// Accepts a time zone identifier only - an IANA id, or a Windows id, which .NET maps.
    /// A place name is rejected with a message telling the model what to send instead, because
    /// it knows perfectly well which zone a city is in and guessing here would be worse.
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
            throw new ArgumentException(
                $"'{requested}' is not a time zone identifier. Pass the zone itself, such as " +
                "'America/New_York' or 'Europe/London' - not a city or region name.",
                nameof(timeZone),
                ex);
        }
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
