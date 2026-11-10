using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monody.App.Options;
using Monody.Domain.Extensions;
using ZiggyCreatures.Caching.Fusion;

namespace Monody.App;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCache(this IServiceCollection services, IConfiguration configuration)
    {
        var cacheOptions = services.ApplyValidatedOptions<CacheOptions>(configuration, "Cache");

        var cacheBuilder = services
            .AddFusionCache()
            .WithOptions(options =>
            {
                options.CacheKeyPrefix = typeof(ServiceCollectionExtensions).Assembly.GetName().Name;
                options.DefaultEntryOptions.SkipBackplaneNotifications = true;
            });

        if (!string.IsNullOrWhiteSpace(cacheOptions.RedisConfiguration))
        {
            cacheBuilder.WithStackExchangeRedisBackplane(options =>
            {
                options.Configuration = cacheOptions.RedisConfiguration;
            });
        }

        return services;
    }
}
