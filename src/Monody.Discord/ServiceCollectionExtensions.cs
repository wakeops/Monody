using Discord.Addons.Hosting;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monody.Discord.Services;

namespace Monody.Discord;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscord(this IServiceCollection services)
    {
        services.AddOptionsWithValidateOnStart<DiscordOptions>()
            .BindConfiguration("Discord")
            .ValidateDataAnnotations();

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
            config.DefaultRunMode = global::Discord.Interactions.RunMode.Async;
        });

        services
            .AddHostedService<InteractionHandler>()
            .AddHostedService<BotStatusService>();

        return services;
    }
}
