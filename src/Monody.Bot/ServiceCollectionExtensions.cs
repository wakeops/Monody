using System;
using System.Linq;
using System.Reflection;
using Discord.Addons.Hosting;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monody.Bot.Options;
using Monody.Bot.Services;
using Monody.Domain.Extensions;
using Monody.Domain.Module;
using ZiggyCreatures.Caching.Fusion;

namespace Monody.Bot;

internal static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscord(this IServiceCollection services)
    {
        services.RegisterOptions<DiscordOptions>("Discord");

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
            .AddHostedService<CommandHandler>()
            .AddHostedService<BotStatusService>();

        return services;
    }

    public static IServiceCollection AddCache(this IServiceCollection services, IConfiguration configuration)
    {
        var cacheOptions = configuration.GetRequiredOptions<CacheOptions>("Cache");

        services.Configure<LoggerFilterOptions>(options =>
        {
            options.AddFilter("ZiggyCreatures.Caching.Fusion", LogLevel.Error);
        });

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

    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration)
    {
        using var provider = services.BuildServiceProvider();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("ModuleBootstrap");

        // Find all non-abstract types that inherit ModuleInitializer across loaded assemblies
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var assemblyTypes = assemblies
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null)!; }
            });

        var moduleTypes = assemblyTypes
            .Where(t => t is not null && typeof(ModuleInitializer).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            .Distinct()
            .ToList();

        foreach (var type in moduleTypes)
        {
            logger.Log_LoadingModule(type.Assembly.GetName().Name);

            if (Activator.CreateInstance(type, nonPublic: true) is ModuleInitializer module)
            {
                module.AddModuleServices(services, configuration);
            }
            else
            {
                logger.Log_FailedToInitializeModule(type.Assembly.GetName().Name);
            }
        }

        return services;
    }
}

internal static partial class ServiceCollectionExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Loading module: {ModuleName}")]
    private static partial void Log_LoadingModule(this ILogger logger, string moduleName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to initialize module: {ModuleName}")]
    private static partial void Log_FailedToInitializeModule(this ILogger logger, string moduleName);
}
