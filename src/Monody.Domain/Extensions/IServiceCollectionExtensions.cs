using Microsoft.Extensions.DependencyInjection;

namespace Monody.Domain.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection RegisterOptions<T>(this IServiceCollection services, string sectionName)
        where T : class
    {
        services
            .AddOptions<T>()
            .BindConfiguration(sectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
