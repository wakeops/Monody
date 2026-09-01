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
    private readonly TimeZoneResolver _timeZoneResolver;
    private readonly TimeProvider _timeProvider;

    // Exactly one constructor: Semantic Kernel activates plugins through ActivatorUtilities,
    // which throws when more than one overload can be satisfied from the container.
    public CurrentTimePlugin(TimeZoneResolver timeZoneResolver, TimeProvider timeProvider)
    {
        _timeZoneResolver = timeZoneResolver;
        _timeProvider = timeProvider;
    }

    [KernelFunction("current_time")]
    [Description(
        "Returns the current date and time, optionally for a given time zone or place. Accepts an " +
        "IANA id or any place name, including ones that are not zone names such as 'Raleigh, NC'. " +
        "Always use this instead of guessing what the current date or time is.")]
    public async Task<CurrentTimeToolResponse> GetCurrentTimeAsync(CurrentTimeToolRequest request, CancellationToken cancellationToken = default)
    {
        var zone = await _timeZoneResolver.ResolveAsync(request?.TimeZone, cancellationToken);

        var utcNow = _timeProvider.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(utcNow, zone);
        var abbreviation = GetAbbreviation(zone, localNow);

        var description = localNow.ToString("dddd, dd MMMM yyyy HH:mm", CultureInfo.InvariantCulture);

        return new CurrentTimeToolResponse
        {
            TimeZone = zone.Id,
            LocalTime = localNow.ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture),
            LocalTimeDescription = string.IsNullOrEmpty(abbreviation) ? description : $"{description} {abbreviation}",
            UtcTime = utcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            UtcOffset = localNow.Offset.ToString(localNow.Offset < TimeSpan.Zero ? "'-'hh':'mm" : "'+'hh':'mm", CultureInfo.InvariantCulture),
            Abbreviation = abbreviation,
            IsDaylightSavingTime = zone.IsDaylightSavingTime(utcNow),
            DiscordTimestamp = $"<t:{utcNow.ToUnixTimeSeconds()}:F>"
        };
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
