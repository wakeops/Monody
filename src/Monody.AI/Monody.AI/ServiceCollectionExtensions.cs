using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monody.AI.Agents;
using Monody.AI.Domain.Abstractions;
using Monody.AI.Options;
using Monody.AI.Provider;
using Monody.AI.Provider.OpenAI;
using Monody.AI.Services;
using Monody.AI.Services.Abstractions;
using Monody.AI.Tools;
using Monody.Domain.Extensions;

namespace Monody.AI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMonodyAI(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddProvider(configuration);

        services.AddDefaultTools();
        
        services.AddAgents();

        services.AddSingleton<IChatCompletionService, ChatCompletionService>();

        return services;
    }

    private static void AddAgents(this IServiceCollection services)
    {
        services.AddTransient<IMonodyAgent, MonodyAgent>();
        services.AddTransient<IResearchAgent, ResearchAgent>();
    }

    private static void AddProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var aiOpts = services.ApplyValidatedOptions<AIOptions>(configuration, "AIOptions");

        services.AddAIProviderServices();

        switch (aiOpts.ChatCompletionProvider.ToLowerInvariant())
        {
            case "openai":
                services.AddOpenAIProvider(configuration);
                break;
            default: throw new NotSupportedException($"The specified chat completion provider '{aiOpts.ChatCompletionProvider}' is not supported.");
        }
    }

    private static IServiceCollection AddOpenAIProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var openAiConfig = services.ApplyValidatedOptions<OpenAIConfiguration>(configuration, "AIOptions:Providers:OpenAI");

        services.AddOpenAI(options =>
        {
            options.ApiKey = openAiConfig.ApiKey;
            options.ChatModel = openAiConfig.ChatModel;
            options.ImageModel = openAiConfig.ImageModel;
        });

        return services;
    }
}
