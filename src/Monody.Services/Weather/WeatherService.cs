using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DarkSky.Models;
using DarkSky.Services;
using Microsoft.Extensions.Logging;
using Monody.Services.Weather.Models;
using Monody.Services.Weather.Utils;
using ZiggyCreatures.Caching.Fusion;

namespace Monody.Services.Weather;

public class WeatherService
{
    private readonly DarkSkyService _darkSky;
    private readonly IFusionCache _cache;
    private readonly ILogger<WeatherService> _logger;

    private readonly TimeSpan _forecastCacheExpiration = TimeSpan.FromMinutes(10);

    public WeatherService(DarkSkyService darkSkyService, IFusionCache cache, ILogger<WeatherService> logger)
    {
        _darkSky = darkSkyService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ForecastData<ForecastNow>> GetCurrentForecastAsync(double latitude, double longitude)
    {
        var forecast = await GetForecastAsync(latitude, longitude);
        if (forecast == null)
        {
            return null;
        }

        var alerts = ConvertAlerts(forecast.Alerts);

        var temp = forecast.Currently.Temperature.GetValueOrDefault();
        var humidity = forecast.Currently.Humidity.GetValueOrDefault() * 100;
        var windSpeed = forecast.Currently.WindSpeed.GetValueOrDefault();
        var heatIndex = HeatIndexCalculator.Calculate(temp, humidity);
        var windChill = WindChillCalculator.Calculate(temp, windSpeed);
        var cardinalWindBearing = WindBearingConverter.ConvertToWindDirection(forecast.Currently.WindBearing);

        var currentDay = forecast.Daily.Data[0];

        return new ForecastData<ForecastNow>
        {
            TimeZone = forecast.TimeZone,
            Data = new ForecastNow
            {
                Condition = forecast.Currently.Summary,
                Temperature = temp,
                Humidity = humidity,
                WindChill = windChill,
                WindSpeed = windSpeed,
                WindGust = currentDay.WindGust.GetValueOrDefault(),
                WindBearing = forecast.Currently.WindBearing.GetValueOrDefault(),
                CardinalWindBearing = cardinalWindBearing,
                ForecastHigh = currentDay.TemperatureHigh.GetValueOrDefault(),
                ForecastLow = currentDay.TemperatureLow.GetValueOrDefault(),
                HeatIndex = heatIndex,
                Icon = forecast.Currently.Icon,
                UVIndex = currentDay.UvIndex.GetValueOrDefault(),
                PrecipitationProbability = currentDay.PrecipProbability.GetValueOrDefault(),
                PrecipitationType = currentDay.PrecipType,
                PrecipitationIntensity = currentDay.PrecipIntensity.GetValueOrDefault(),
                PrecipitationIntensityMax = currentDay.PrecipIntensityMax,
                SnowAccumulation = currentDay.PrecipAccumulation.GetValueOrDefault(),
                Alerts = alerts
            }
        };
    }

    public async Task<ForecastData<List<ForecastHour>>> GetHourlyForecastAsync(double latitude, double longitude)
    {
        var forecast = await GetForecastAsync(latitude, longitude);
        if (forecast == null)
        {
            return null;
        }

        return new ForecastData<List<ForecastHour>>
        {
            TimeZone = forecast.TimeZone,
            Data = [.. forecast.Hourly.Data.Select(a =>
                new ForecastHour
                {
                    Date = a.DateTime,
                    Temperature = a.Temperature.GetValueOrDefault(),
                    FeelsLikeTemperature = a.ApparentTemperature.GetValueOrDefault(),
                    PrecipitationProbability = a.PrecipProbability.GetValueOrDefault() * 100,
                    PrecipitationIntensity = a.PrecipIntensity.GetValueOrDefault(),
                    CloudCover = a.CloudCover.GetValueOrDefault() * 100,
                    Humidity = a.Humidity.GetValueOrDefault() * 100,
                    WindSpeed = a.WindSpeed.GetValueOrDefault(),
                    WindBearing = a.WindBearing,
                    CardinalWindBearing = WindBearingConverter.ConvertToWindDirection(a.WindBearing),
                    Icon = a.Icon,
                    Summary = a.Summary
                }
            )]
        };
    }

    public async Task<ForecastData<List<ForecastDay>>> GetWeeklyForecastAsync(double latitude, double longitude)
    {
        var forecast = await GetForecastAsync(latitude, longitude);
        if (forecast == null)
        {
            return null;
        }

        return new ForecastData<List<ForecastDay>>
        {
            TimeZone = forecast.TimeZone,
            Data = [.. forecast.Daily.Data.Select(a =>
                new ForecastDay
                {
                    Date = a.DateTime,
                    High = a.TemperatureHigh.GetValueOrDefault(),
                    Low = a.TemperatureLow.GetValueOrDefault(),
                    Icon = a.Icon,
                    Summary = a.Summary
                }
            )]
        };
    }

    private async Task<Forecast> GetForecastAsync(double latitude, double longitude)
    {
        return await _cache.GetOrSetAsync(
            $"forecastv2-{latitude}-{longitude}",
            _ => GetForecastFromApiAsync(latitude, longitude),
            _forecastCacheExpiration);
    }

    private async Task<Forecast> GetForecastFromApiAsync(double latitude, double longitude)
    {
        _logger.LogInformation("Fetching forecast for {Latitude}, {Longitude}.", latitude, longitude);

        var result = await _darkSky.GetForecast(latitude, longitude, new OptionalParameters
        {
            MeasurementUnits = "us",
            DataBlocksToExclude = [ExclusionBlocks.Minutely, ExclusionBlocks.Flags]
        });

        if (result?.IsSuccessStatus != true)
        {
            _logger.LogError("Failed to fetch forecast for '{Latitude}, {Longitude}': {ResponseReason}.", latitude, longitude, result?.ResponseReasonPhrase);
            return null;
        }

        return result?.Response;
    }

    private static IEnumerable<WeatherAlert> ConvertAlerts(IEnumerable<Alert> alerts)
    {
        if (alerts == null)
        {
            return [];
        }

        return alerts
            .OrderBy(a => a.ExpiresDateTime)
            .Where(a => !alerts.Any(b => a.Uri == b.Uri && a.ExpiresDateTime < b.ExpiresDateTime))
            .Select(a =>
            {
                return new WeatherAlert
                {
                    IssuedDate = a.DateTime,
                    ExpirationDate = a.ExpiresDateTime,
                    Title = a.Title,
                    Description = a.Description,
                    Uri = a.Uri
                };
            });
    }
}