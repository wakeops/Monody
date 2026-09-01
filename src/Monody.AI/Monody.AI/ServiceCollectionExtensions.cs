using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Monody.AI.Agents;
using Monody.AI.Options;
using Monody.AI.Tools;
using Monody.AI.Tools.Abstractions;
using Monody.AI.Tools.Capabilities.Calculator;
using Monody.AI.Tools.Capabilities.CurrentTime;
using Monody.AI.Tools.Capabilities.FetchBlueSky;
using Monody.AI.Tools.Capabilities.FetchUrl;
using Monody.AI.Tools.Capabilities.Geocode;
using Monody.AI.Tools.Capabilities.Memory;
using Monody.AI.Tools.Capabilities.GetDiscordMessage;
using Monody.AI.Tools.Capabilities.GetDiscordMessageHistory;
using Monody.AI.Tools.Capabilities.Reminders;
using Monody.AI.Tools.Capabilities.ResearchAssistant;
using Monody.AI.Tools.Capabilities.Weather;
using Monody.AI.Tools.Capabilities.WebSearch;
using Monody.Data;
using Monody.Domain.Extensions;

namespace Monody.AI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMonodyAI(this IServiceCollection services, IConfiguration configuration)
    {
        var openAiConfig = services.ApplyValidatedOptions<OpenAIConfiguration>(configuration, "AIOptions:Providers:OpenAI");

        services.AddMonodyData(configuration);

        // Tools that touch per-user data read the caller from here, never from the model.
        services.AddSingleton<IInvocationContext, AsyncLocalInvocationContext>();
        services.AddSingleton<TimeZoneResolver>();

        var kernelBuilder = services.AddKernel();

        kernelBuilder.AddOpenAIChatCompletion(openAiConfig.ChatModel, openAiConfig.ApiKey);
        kernelBuilder.AddOpenAITextToImage(openAiConfig.ImageModel, openAiConfig.ApiKey);

        kernelBuilder.Plugins.AddFromType<WeatherPlugin>();
        kernelBuilder.Plugins.AddFromType<WebSearchPlugin>();
        kernelBuilder.Plugins.AddFromType<FetchUrlPlugin>();
        kernelBuilder.Plugins.AddFromType<FetchBlueSkyPlugin>();
        kernelBuilder.Plugins.AddFromType<GetDiscordMessagePlugin>();
        kernelBuilder.Plugins.AddFromType<GetDiscordMessageHistoryPlugin>();
        kernelBuilder.Plugins.AddFromType<ResearchAssistantPlugin>();
        kernelBuilder.Plugins.AddFromType<GeocodePlugin>();
        kernelBuilder.Plugins.AddFromType<CurrentTimePlugin>();
        kernelBuilder.Plugins.AddFromType<MathPlugin>();
        kernelBuilder.Plugins.AddFromType<MemoryPlugin>();
        kernelBuilder.Plugins.AddFromType<ReminderPlugin>();

        services.AddTransient<IResearchAgent, ResearchAgent>();
        services.AddSingleton<IFunctionInvocationFilter, BareStringRequestCoercionFilter>();

        return services;
    }
}
