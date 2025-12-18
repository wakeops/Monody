using System;
using DarkSky.Services;
using Geo.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monody.Domain.Extensions;
using Monody.Services.Geocode;
using Monody.Services.Weather;

namespace Monody.Services;

public static class ServiceCollectionExtensions
{
    private const string _pirateWeatherApi = "https://api.pirateweather.net/";

    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGeocodingServices(configuration);
        services.AddWeatherServices(configuration);

        return services;
    }

    private static void AddGeocodingServices(this IServiceCollection services, IConfiguration configuration)
    {
        var opts = services.ApplyValidatedOptions<GeocodeOptions>(configuration, "Services:Geocode");

        services.AddHereGeocoding()
            .AddKey(opts.HereApiKey);

        services.AddSingleton<GeocodeService>();
    }

    private static void AddWeatherServices(this IServiceCollection services, IConfiguration configuration)
    {
        var opts = services.ApplyValidatedOptions<WeatherOptions>(configuration, "Services:Weather");

        services.AddTransient(sp =>
            new DarkSkyService(
                opts.PirateWeatherApiKey,
                baseUri: new Uri(_pirateWeatherApi),
                jsonSerializerService: new DarkSkyJsonSerializerService()));
        
        services.AddSingleton<WeatherService>();
    }
}
