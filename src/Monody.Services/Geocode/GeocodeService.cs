using System;
using System.Linq;
using System.Threading.Tasks;
using Geo.Here;
using Geo.Here.Models.Parameters;
using Microsoft.Extensions.Logging;
using Monody.Services.Geocode.Models;
using ZiggyCreatures.Caching.Fusion;

namespace Monody.Services.Geocode;

public class GeocodeService
{
    private readonly IHereGeocoding _hereGeocoding;
    private readonly IFusionCache _cache;
    private readonly ILogger<GeocodeService> _logger;

    private readonly TimeSpan _geocodeCacheExpiration = TimeSpan.FromHours(1);

    public GeocodeService(IHereGeocoding hereGeocoding, IFusionCache cache, ILogger<GeocodeService> logger)
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
        _logger.LogInformation("Fetching location for '{Location}'", locationQuery);

        try
        {
            var geocodeResponse = await _hereGeocoding.GeocodingAsync(new GeocodeParameters { Query = locationQuery });

            // Best match wins; US results break ties, since most users are searching US locations.
            var location = geocodeResponse.Items
                .OrderByDescending(a => a.Scoring.QueryScore)
                .ThenByDescending(a => a.Address.CountryCode == "USA")
                .FirstOrDefault();

            if (location == null)
            {
                _logger.LogWarning("No geocode results for '{Location}'", locationQuery);
                return null;
            }

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
            _logger.LogError(ex, "Failed to resolve geocode: '{Location}'", locationQuery);
        }

        return null;
    }
}
