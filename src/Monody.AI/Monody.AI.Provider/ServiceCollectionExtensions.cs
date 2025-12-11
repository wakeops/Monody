using Microsoft.Extensions.DependencyInjection;
using Monody.AI.Provider.Services;
using Monody.AI.Provider.Services.Abstractions;

namespace Monody.AI.Provider;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAIProviderServices(this IServiceCollection services)
    {
        services.AddSingleton<IToolDispatcher, ToolDispatcher>();

        return services;
    }
}
