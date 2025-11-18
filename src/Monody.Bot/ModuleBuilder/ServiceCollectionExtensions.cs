using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monody.Bot.ModuleBuilder.Models;

namespace Monody.Bot.ModuleBuilder;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration)
    {
        var moduleConfigs = BuildModules(services, configuration);

        services.Configure<ModuleLoaderConfig>(options =>
        {
            options.ModuleConfigs = moduleConfigs;
        });

        return services;
    }

    private static IEnumerable<ModuleConfig> BuildModules(IServiceCollection services, IConfiguration configuration)
    {
        var moduleRoot = AppContext.BaseDirectory;

        var builder = new ModuleBuilder(services);

        return builder.LoadModuleAssembliesFromPath(services, configuration, moduleRoot);
    }
}
