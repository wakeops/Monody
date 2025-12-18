using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monody.Bot.ModuleBuilder.Models;

namespace Monody.Bot.ModuleBuilder;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddModulesFromAssembly(this IServiceCollection services, IConfiguration configuration, Assembly assembly)
    {
        var builder = new ModuleBuilder(services);

        var moduleConfigs = builder.LoadModulesFromAssembly(configuration, assembly);

        services.Configure<ModuleLoaderConfig>(options =>
        {
            options.ModuleConfigs ??= [];
            options.ModuleConfigs.AddRange(moduleConfigs);
        });

        return services;
    }
}
