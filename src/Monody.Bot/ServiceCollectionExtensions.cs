using System;
using System.IO;
using System.Linq;
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

    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration, string modulesRoot)
    {
        if (!Directory.Exists(modulesRoot))
        {
            return services;
        }

        using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("ModuleBootstrap");

        foreach (var modulePath in Directory.EnumerateDirectories(modulesRoot))
        {
            LoadModuleAssembly(services, configuration, logger, modulePath);
        }

        return services;
    }

    private static void LoadModuleAssembly(IServiceCollection services, IConfiguration configuration, ILogger logger, string modulePath)
    {
        var moduleAsms = Directory.EnumerateFiles(modulePath, "Monody.Module.*.dll", SearchOption.AllDirectories);

        foreach (var moduleAsm in moduleAsms)
        {
            var mlc = new ModuleLoadContext(moduleAsm);
            var asm = mlc.LoadFromAssemblyName(new(Path.GetFileNameWithoutExtension(moduleAsm)));

            foreach (var type in asm.GetTypes().Where(t => typeof(ModuleInitializer).IsAssignableFrom(t)))
            {
                logger.Log_LoadingModule(asm.GetName().Name);

                if (Activator.CreateInstance(type) is ModuleInitializer module)
                {
                    module.AddModuleServices(services, configuration);
                }
                else
                {
                    logger.Log_FailedToInitializeModule(asm.GetName().Name);
                    throw new ApplicationException($"Failed to initialize module: {type.Assembly.GetName().Name}");
                }
            }
        }
    }
}

internal static partial class ServiceCollectionExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Loading module: {ModuleName}")]
    private static partial void Log_LoadingModule(this ILogger logger, string moduleName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to initialize module: {ModuleName}")]
    private static partial void Log_FailedToInitializeModule(this ILogger logger, string moduleName);
}
