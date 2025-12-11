using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Monody.AI.Tools.Capabilities.FetchBlueSky;
using Monody.AI.Tools.Capabilities.WebSearch;
using Monody.AI.Tools.ToolHandler;

namespace Monody.AI.Tools;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultTools(this IServiceCollection services)
    {
        services.AddToolHandlers(typeof(IToolHandler).Assembly);

        // Tool dependencies
        services.AddHttpClient();
        services.AddSingleton<BlueSkyService>();
        services.AddWebSearchTool();

        return services;
    }

    public static IServiceCollection AddToolHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        if (assemblies == null || assemblies.Length == 0)
        {
            assemblies = [Assembly.GetExecutingAssembly()];
        }

        var handlerType = typeof(IToolHandler);

        var types = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface && handlerType.IsAssignableFrom(t));

        foreach (var type in types)
        {
            services.AddToolHandler(type);
        }

        return services;
    }

    public static IServiceCollection AddToolHandler<THandlerType>(this IServiceCollection services)
        where THandlerType : class, IToolHandler
        => services.AddToolHandler(typeof(THandlerType));

    public static IServiceCollection AddToolHandler(this IServiceCollection services, Type handler)
        => services.AddSingleton(typeof(IToolHandler), handler);
}
