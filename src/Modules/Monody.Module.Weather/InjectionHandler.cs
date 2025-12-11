using System;
using DarkSky.Services;
using Geo.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monody.Domain.Module;
using Monody.Module.Weather.Services;

namespace Monody.Module.Weather;

public class InjectionHandler : ModuleInjectionHandler
{
    public override void AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsWithValidateOnStart<WeatherOptions>()
            .BindConfiguration("Module:Weather");

        var opts = configuration.GetSection("Module:Weather")
            .Get<WeatherOptions>();

        services.AddHereGeocoding()
            .AddKey(opts.HereApiKey);

        services.AddTransient(sp => new DarkSkyService(
            opts.PirateWeatherApiKey,
            baseUri: new Uri(Constants.PirateWeatherApi),
            jsonSerializerService: new DarkSkyJsonSerializerService()));

        services.AddSingleton<LocationService>();
        services.AddSingleton<WeatherService>();
    }
}
