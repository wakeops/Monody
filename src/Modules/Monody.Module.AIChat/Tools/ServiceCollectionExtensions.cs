using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Monody.Module.AIChat.Tools.Abstractions;
using Monody.Module.AIChat.Tools.Attributes;
using Monody.Module.AIChat.Tools.Definitions.FetchBlueSky;

namespace Monody.Module.AIChat.Tools;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterChatTools(this IServiceCollection services)
    {
        InjectToolRunners(services);

        services.AddTransient<ChatToolProvider>();

        // --- Tool Dependencies ---

        // FetchBlueSky
        services.AddTransient<BlueSkyService>();

        return services;
    }

    private static void InjectToolRunners(IServiceCollection services)
    {
        var toolRunners = typeof(ChatToolRunner).Assembly
            .GetTypes()
            .Where(x => !x.IsInterface && !x.IsAbstract && x.IsAssignableTo(typeof(ChatToolRunner)))
            .ToList();

        var toolNames = new List<string>();

        foreach (var toolRunner in toolRunners)
        {
            var toolDefinition = toolRunner.GetCustomAttribute<ChatToolRunnerAttribute>();
            var name = toolDefinition.Name ?? toolRunner.Name;

            services.AddKeyedTransient(typeof(ChatToolRunner), name, toolRunner);

            toolNames.Add(toolDefinition.Name);
        }

        services.PostConfigure<OpenAIOptions>(o => o.Tools = toolNames);
    }
}
