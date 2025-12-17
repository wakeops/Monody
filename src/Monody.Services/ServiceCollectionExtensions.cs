using System;
using DarkSky.Services;
using Geo.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        var optPath = "Services:Geocode";

        services.AddOptionsWithValidateOnStart<GeocodeOptions>()
           .BindConfiguration(optPath);

        var opts = configuration.GetSection(optPath)
            .Get<GeocodeOptions>();

        services.AddSingleton<GeocodeService>();

        services.AddHereGeocoding()
            .AddKey(opts.HereApiKey);
    }

    private static void AddWeatherServices(this IServiceCollection services, IConfiguration configuration)
    {
        var optPath = "Services:Weather";

        services.AddOptionsWithValidateOnStart<WeatherOptions>()
           .BindConfiguration(optPath);

        var opts = configuration.GetSection(optPath)
            .Get<WeatherOptions>();

        services.AddTransient(sp => new DarkSkyService(
                opts.PirateWeatherApiKey,
                baseUri: new Uri(_pirateWeatherApi),
                jsonSerializerService: new DarkSkyJsonSerializerService()));
        
        services.AddSingleton<WeatherService>();
    }
}
