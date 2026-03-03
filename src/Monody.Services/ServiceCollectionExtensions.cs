using System;
using DarkSky.Services;
using Geo.Extensions.DependencyInjection;
using Google.Apis.CustomSearchAPI.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monody.Domain.Extensions;
using Monody.Services.BlueSky;
using Monody.Services.Geocode;
using Monody.Services.Weather;
using Monody.Services.WebSearch;

namespace Monody.Services;

public static class ServiceCollectionExtensions
{
    private const string _pirateWeatherApi = "https://api.pirateweather.net/";

    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGeocodingServices(configuration);
        services.AddWeatherServices(configuration);
        services.AddBlueSkyServices();
        services.AddWebSearchServices(configuration);

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

    private static void AddBlueSkyServices(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddSingleton<BlueSkyService>();
    }

    private static void AddWebSearchServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsWithValidateOnStart<WebSearchOptions>()
            .BindConfiguration("Services:WebSearch");

        services.AddSingleton<CustomSearchAPIService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<WebSearchOptions>>().Value;

            return new CustomSearchAPIService(new BaseClientService.Initializer
            {
                ApiKey = options.GoogleApiKey
            });
        });

        services.AddTransient<GoogleSearchService>();
    }
}
