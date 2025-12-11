using Discord.Addons.Hosting;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monody.Bot.Options;
using Monody.Bot.Services;
using Monody.Domain.Extensions;
using ZiggyCreatures.Caching.Fusion;

namespace Monody.Bot;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscord(this IServiceCollection services)
    {
        services.AddOptionsWithValidateOnStart<DiscordOptions>()
            .BindConfiguration("Discord");

        services.AddDiscordHost((config, sp) =>
        {
            var opts = sp.GetRequiredService<IOptions<DiscordOptions>>().Value;

            config.Token = opts.Token;

            config.SocketConfig = new()
            {
                GatewayIntents = opts.GatewayIntents,
                LogLevel = opts.LogSeverity,
                AlwaysDownloadUsers = false,
                UseInteractionSnowflakeDate = false
            };
        });

        services.AddCommandService((config, _) =>
        {
            config.DefaultRunMode = RunMode.Async;
            config.CaseSensitiveCommands = false;
        });

        services.AddInteractionService((config, sp) =>
        {
            var opts = sp.GetRequiredService<IOptions<DiscordOptions>>().Value;

            config.LogLevel = opts.LogSeverity;
            config.UseCompiledLambda = true;
        });

        services
            .AddHostedService<InteractionHandler>()
            .AddHostedService<BotStatusService>()
            .AddHostedService<ModuleLoaderService>();

        return services;
    }

    public static IServiceCollection AddCache(this IServiceCollection services, IConfiguration configuration)
    {
        var cacheOptions = configuration.GetRequiredOptions<CacheOptions>("Cache");

        var cacheBuilder = services
            .AddFusionCache()
            .WithOptions(options =>
            {
                options.CacheKeyPrefix = typeof(ServiceCollectionExtensions).Assembly.GetName().Name;
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
