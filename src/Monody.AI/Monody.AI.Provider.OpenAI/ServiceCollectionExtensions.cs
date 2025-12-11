using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monody.AI.Provider.OpenAI.Services;
using OpenAI.Chat;
using OpenAI.Images;

namespace Monody.AI.Provider.OpenAI;

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
        services.AddSingleton<IChatCompletionProvider, OpenAIProvider>();

        return services;
    }
}
