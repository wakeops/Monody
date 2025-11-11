using System;
using DarkSky.Services;
using Geo.Here.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monody.Domain.Extensions;
using Monody.Domain.Module;
using Monody.Module.Weather.Services;

namespace Monody.Module.Weather;

public class Initializer : ModuleInitializer
{
    public override void AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterOptions<WeatherOptions>("Weather");

        var opts = configuration.GetRequiredOptions<WeatherOptions>("Weather");

        services.AddHereServices(builder => builder.UseKey(opts.HereApiKey));

        services.AddTransient(sp => new DarkSkyService(
            opts.PirateWeatherApiKey,
            baseUri: new Uri(Constants.PirateWeatherApi),
            jsonSerializerService: new DarkSkyJsonSerializerService()));

        services.AddSingleton<LocationService>();
        services.AddSingleton<WeatherService>();
    }
}
