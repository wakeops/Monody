using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Monody.Domain.Extensions;

public static class IServiceCollectionExtensions
{
    public static T ApplyValidatedOptions<T>(this IServiceCollection services, IConfiguration configuration, string configSectionPath)
        where T : class
    {
        services.AddOptionsWithValidateOnStart<T>()
           .BindConfiguration(configSectionPath);

        return configuration.GetSection(configSectionPath)
            .Get<T>();
    }
}
