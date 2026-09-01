using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monody.Bot.Modules;

namespace Monody.Bot.ModuleBuilder;

internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Runs every <see cref="ModuleInjectionHandler"/> in the assembly so each Discord module
    /// can register its own services.
    /// </summary>
    public static IServiceCollection AddModulesFromAssembly(this IServiceCollection services, IConfiguration configuration, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(ModuleInjectionHandler).IsAssignableFrom(t));

        foreach (var type in handlerTypes)
        {
            var handler = (ModuleInjectionHandler)Activator.CreateInstance(type);
            handler.AddModuleServices(services, configuration);
        }

        return services;
    }
}
