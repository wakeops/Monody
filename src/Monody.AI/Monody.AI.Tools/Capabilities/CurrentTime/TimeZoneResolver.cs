using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeoTimeZone;
using Monody.Services.Geocode;

namespace Monody.AI.Tools.Capabilities.CurrentTime;

/// <summary>
/// Turns whatever the model sends into a <see cref="TimeZoneInfo"/>.
/// </summary>
/// <remarks>
/// The model is asked for an IANA id but frequently sends a place instead, and most places are
/// not zone names: "Raleigh, NC" is America/New_York. Matching zone ids alone therefore fails
/// for almost everywhere, so anything unrecognised is geocoded and the coordinates mapped to a
/// zone. The cheap checks run first so the common cases cost nothing.
/// </remarks>
public sealed class TimeZoneResolver
{
    private readonly GeocodeService _geocodeService;

    public TimeZoneResolver(GeocodeService geocodeService)
    {
        _geocodeService = geocodeService;
    }

    public async Task<TimeZoneInfo> ResolveAsync(string timeZoneOrPlace, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(timeZoneOrPlace))
        {
            return TimeZoneInfo.Utc;
        }

        var requested = timeZoneOrPlace.Trim();

        return TryExactId(requested)
               ?? TryCityName(requested)
               ?? await TryGeocodeAsync(requested, cancellationToken)
               ?? throw new ArgumentException(
                   $"Could not work out a time zone for '{requested}'. Give an IANA id such as " +
                   "'Europe/London', or a place name that can be looked up.",
                   nameof(timeZoneOrPlace));
    }

    /// <summary>An IANA id, or a Windows id, exactly as the system knows it.</summary>
    private static TimeZoneInfo TryExactId(string requested)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(requested);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return null;
        }
    }

    /// <summary>Matches the city segment of an id, so "tokyo" finds Asia/Tokyo without a lookup.</summary>
    private static TimeZoneInfo TryCityName(string requested)
    {
        var normalized = requested.Replace(' ', '_');

        return TimeZoneInfo.GetSystemTimeZones()
            .Where(zone => zone.Id.Contains('/'))
            // Ordered so a given name always resolves to the same zone.
            .OrderBy(zone => zone.Id, StringComparer.Ordinal)
            .FirstOrDefault(zone =>
                zone.Id[(zone.Id.LastIndexOf('/') + 1)..].Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Anywhere else: geocode the place, then map the coordinates to a zone.</summary>
    private async Task<TimeZoneInfo> TryGeocodeAsync(string place, CancellationToken cancellationToken)
    {
        var location = await _geocodeService.GetGeocodeForLocationStringAsync(place);

        if (location?.Coordinates is null)
        {
            return null;
        }

        var id = TimeZoneLookup.GetTimeZone(location.Coordinates.Latitude, location.Coordinates.Longitude).Result;

        return TryExactId(id);
    }
}
