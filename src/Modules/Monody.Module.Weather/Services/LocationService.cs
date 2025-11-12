using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geo.Here;
using Geo.Here.Models.Parameters;
using Geo.Here.Models.Responses;
using Microsoft.Extensions.Logging;
using Monody.Module.Weather.Models;
using ZiggyCreatures.Caching.Fusion;

namespace Monody.Module.Weather.Services;

public class LocationService
{
    private readonly IHereGeocoding _hereGeocoding;
    private readonly IFusionCache _cache;
    private readonly ILogger<LocationService> _logger;

    private readonly TimeSpan _geocodeCacheExpiration = TimeSpan.FromHours(1);

    public LocationService(IHereGeocoding hereGeocoding, IFusionCache cache, ILogger<LocationService> logger)
    {
        _hereGeocoding = hereGeocoding;
        _cache = cache;
        _logger = logger;
    }

    public async Task<LocationDetails> GetGeocodeForLocationStringAsync(string locationQuery)
    {
        return await _cache.GetOrSetAsync(
            $"geocodev2-{locationQuery}",
            _ => SearchGeocodeByLocationFromApiAsync(locationQuery),
            _geocodeCacheExpiration);
    }

    private async Task<LocationDetails> SearchGeocodeByLocationFromApiAsync(string locationQuery)
    {
        _logger.Log_FetchingGecode(locationQuery);

        try
        {
            var geocodeResponse = await _hereGeocoding.GeocodingAsync(new GeocodeParameters { Query = locationQuery });

            var location = geocodeResponse.Items.OrderByDescending(a => a, new GeocodeComparer()).ToList().First();

            return new LocationDetails
            {
                Coordinates = new Coordinates
                {
                    Latitude = location.Position.Latitude,
                    Longitude = location.Position.Longitude
                },
                Country = location.Address.CountryName,
                Region = location.Address.State,
                City = location.Address.City
            };
        }
        catch (Exception ex)
        {
            _logger.Log_GecodeFailure(ex, locationQuery);
        }

        return null;
    }

    private class GeocodeComparer : IComparer<GeocodeLocation>
    {
        public int Compare(GeocodeLocation x, GeocodeLocation y)
        {
            if (x.Scoring.QueryScore == y.Scoring.QueryScore && x.Address.CountryCode != y.Address.CountryCode && x.Address.CountryCode == "USA")
            {
                return 1;
            }

            return x.Scoring.QueryScore.CompareTo(y.Scoring.QueryScore);
        }
    }
}

internal static partial class LocationServiceLogging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching location for '{Location}'")]
    public static partial void Log_FetchingGecode(this ILogger logger, string location);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to resolve geocode: '{Location}'")]
    public static partial void Log_GecodeFailure(this ILogger logger, Exception ex, string location);
}
