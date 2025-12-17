using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Monody.AI.Tools.ToolHandler;
using Monody.Services.Geocode;
using Monody.Services.Geocode.Models;
using Monody.Services.Weather;

namespace Monody.AI.Tools.Capabilities.Weather;

internal class WeatherTool : ToolHandler<WeatherToolRequest, WeatherToolResponse>
{
    private readonly WeatherService _weatherService;
    private readonly GeocodeService _geocodeService;

    public WeatherTool(WeatherService weatherService, GeocodeService geocodeService)
    {
        _weatherService = weatherService;
        _geocodeService = geocodeService;
    }

    public override string Name => "weather";

    public override string Description => "Return the current weather for a given latitude and longitude. Temperature values are in fahrenheit.";

    protected override async Task<WeatherToolResponse> HandleAsync(WeatherToolRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        (double latitude, double longitude, LocationDetails geocode) = await ResolveCoordinatesAsync(request);

        object forecast = request.Range switch
        {
            WeatherRange.Current => await _weatherService.GetCurrentForecastAsync(latitude, longitude, request.Units),
            WeatherRange.Daily => await _weatherService.GetDailyForecastAsync(latitude, longitude, request.Days ?? 7, request.Units),
            WeatherRange.Hourly => await _weatherService.GetHourlyForecastAsync(latitude, longitude, request.Units),
            _ => throw new ArgumentException("Unsupported weather range.", nameof(request.Range)),
        };

        return new WeatherToolResponse
        {
            GeocodeData = geocode,
            WeatherData = forecast
        };
    }

    private async Task<(double Latitude, double Longitude, LocationDetails Geocode)> ResolveCoordinatesAsync(WeatherToolRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.LocationQuery))
        {
            LocationDetails geocode = await _geocodeService.GetGeocodeForLocationStringAsync(request.LocationQuery);
            return (geocode.Coordinates.Latitude, geocode.Coordinates.Longitude, geocode);
        }

        return (request.Latitude!.Value, request.Longitude!.Value, null);
    }

    private static void ValidateRequest(WeatherToolRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hasLocationQuery = !string.IsNullOrWhiteSpace(request.LocationQuery);
        var hasLatLon = request.Latitude.HasValue && request.Longitude.HasValue;
        var hasPartialLatLon = request.Latitude.HasValue ^ request.Longitude.HasValue;

        if (hasPartialLatLon)
        {
            throw new ArgumentNullException("Both Latitude and Longitude must be provided together.");
        }

        if (hasLocationQuery == hasLatLon) // both false OR both true
        {
            throw new ArgumentNullException("Provide either LocationQuery OR Latitude+Longitude (exactly one).");
        }

        if (request.Latitude is < -90 or > 90)
        {
            throw new ArgumentNullException(nameof(request.Latitude), "Latitude must be between -90 and 90.");
        }

        if (request.Longitude is < -180 or > 180)
        {
            throw new ArgumentNullException(nameof(request.Longitude), "Longitude must be between -180 and 180.");
        }
    }
}
