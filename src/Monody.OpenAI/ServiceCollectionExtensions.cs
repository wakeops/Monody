using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monody.OpenAI.Services;
using Monody.OpenAI.ToolHandler;
using Monody.OpenAI.Tools.FetchBlueSky;
using OpenAI.Chat;
using OpenAI.Images;

namespace Monody.OpenAI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAI(this IServiceCollection services, Action<OpenAIConfiguration> configure)
    {
        services.AddOptions<OpenAIConfiguration>()
            .Configure(configure)
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.ChatModel))
                {
                    options.ChatModel = Constants.DefaultChatModel;
                }

                if (string.IsNullOrWhiteSpace(options.ImageModel))
                {
                    options.ImageModel = Constants.DefaultImageModel;
                }
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenAIConfiguration>>().Value;
            return new ChatClient(model: opts.ChatModel, apiKey: opts.ApiKey);
        });

        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenAIConfiguration>>().Value;
            return new ImageClient(model: opts.ImageModel, apiKey: opts.ApiKey);
        });

        services.AddSingleton<OpenAIService>();
        services.AddSingleton<ToolDispatcher>();

        services.AddToolHandlers(typeof(IToolHandler).Assembly);

        // Register tool dependencies
        services.AddHttpClient();
        services.AddSingleton<BlueSkyService>();

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
