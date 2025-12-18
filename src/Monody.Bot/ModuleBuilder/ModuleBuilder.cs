using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monody.Bot.ModuleBuilder.Models;
using Monody.Bot.Modules;

namespace Monody.Bot.ModuleBuilder;

internal class ModuleBuilder
{
    private readonly ILogger _logger;
    private readonly IServiceCollection _services;

    public ModuleBuilder(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();

        _services = services;
        _logger = provider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(ModuleBuilder));
    }

    public IEnumerable<ModuleConfig> LoadModulesFromAssembly(IConfiguration configuration, Assembly assembly)
    {
        var moduleTypes = assembly.GetTypes().Where(t => typeof(ModuleInjectionHandler).IsAssignableFrom(t));

        foreach (var type in moduleTypes)
        {
            var moduleName = type.FullName;

            _logger.LogInformation("Injecting module: {ModuleName}", moduleName);

            if (Activator.CreateInstance(type) is ModuleInjectionHandler module)
            {
                module.AddModuleServices(_services, configuration);
                yield return new ModuleConfig
                {
                    Assembly = assembly,
                    Handler = module,
                    Name = moduleName
                };
                continue;
            }

            throw new ApplicationException($"Failed to inject module: {moduleName}");
        }
    }
}