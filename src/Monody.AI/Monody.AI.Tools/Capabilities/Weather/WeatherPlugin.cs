using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Monody.Services.Geocode;
using Monody.Services.Geocode.Models;
using Monody.Services.Weather;

namespace Monody.AI.Tools.Capabilities.Weather;

public sealed class WeatherPlugin(WeatherService weatherService, GeocodeService geocodeService)
{
    [KernelFunction("weather")]
    [Description("Return the current weather for a given latitude and longitude. Temperature values are in fahrenheit.")]
    public async Task<WeatherToolResponse> GetWeatherAsync(WeatherToolRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var (latitude, longitude, geocode) = await ResolveCoordinatesAsync(request);

        object forecast = request.Range switch
        {
            WeatherRange.Current => await weatherService.GetCurrentForecastAsync(latitude, longitude, request.Units),
            WeatherRange.Daily => await weatherService.GetDailyForecastAsync(latitude, longitude, request.Days ?? 7, request.Units),
            WeatherRange.Hourly => await weatherService.GetHourlyForecastAsync(latitude, longitude, request.Units),
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
        if (string.IsNullOrWhiteSpace(request.LocationQuery))
        {
            return (request.Latitude.Value, request.Longitude.Value, null);
        }

        var geocode = await geocodeService.GetGeocodeForLocationStringAsync(request.LocationQuery)
            ?? throw new InvalidOperationException($"Could not resolve a location for '{request.LocationQuery}'.");

        return (geocode.Coordinates.Latitude, geocode.Coordinates.Longitude, geocode);
    }

    private static void ValidateRequest(WeatherToolRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Latitude.HasValue ^ request.Longitude.HasValue)
        {
            throw new ArgumentException("Both Latitude and Longitude must be provided together.", nameof(request));
        }

        var hasLocationQuery = !string.IsNullOrWhiteSpace(request.LocationQuery);
        var hasCoordinates = request.Latitude.HasValue && request.Longitude.HasValue;

        if (hasLocationQuery == hasCoordinates)
        {
            throw new ArgumentException("Provide either LocationQuery OR Latitude+Longitude (exactly one).", nameof(request));
        }

        if (request.Latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Latitude must be between -90 and 90.");
        }

        if (request.Longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Longitude must be between -180 and 180.");
        }
    }
}
