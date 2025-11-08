using System;
using DarkSky.Services;
using Geo.Here.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monody.Domain.Module;
using Monody.Module.Weather.Services;

namespace Monody.Module.Weather;

public class Initializer : ModuleInitializer
{
    public override void AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<WeatherOptions>()
            .BindConfiguration("Weather")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var opt = configuration.GetSection("Weather").Get<WeatherOptions>();

        services.AddHereServices(builder => builder.UseKey(opt.HereApiKey));

        services.AddTransient(sp => new DarkSkyService(
            opt.PirateWeatherApiKey,
            baseUri: new Uri(Constants.PirateWeatherApi),
            jsonSerializerService: new DarkSkyJsonSerializerService()));

        services.AddSingleton<LocationService>();
        services.AddSingleton<WeatherService>();
    }
}
