using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monody.Bot.ModuleBuilder.Models;
using Monody.Domain.Module;

namespace Monody.Bot.ModuleBuilder;

internal class ModuleBuilder
{
    private const string _moduleRootNamespace = "Monody.Module";

    private readonly ILogger _logger;
    private readonly IServiceCollection _services;

    public ModuleBuilder(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();

        _services = services;
        _logger = provider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(ModuleBuilder));
    }

    public IEnumerable<ModuleConfig> LoadModuleAssembliesFromPath(IServiceCollection services, IConfiguration configuration, string rootModulePath)
    {
        var moduleAssemblyPaths = Directory.EnumerateFiles(rootModulePath, $"{_moduleRootNamespace}.*.dll", SearchOption.AllDirectories);

        var moduleList = new List<ModuleConfig>();
        foreach (var modulePath in moduleAssemblyPaths)
        {
            moduleList.AddRange(LoadModuleAssembly(configuration, modulePath));
        }

        return moduleList;
    }

    public IEnumerable<ModuleConfig> LoadModuleAssembly(IConfiguration configuration, string moduleAssemblyPath)
    {
        var mlc = new ModuleLoadContext(moduleAssemblyPath);
        var asm = mlc.LoadFromAssemblyName(new(Path.GetFileNameWithoutExtension(moduleAssemblyPath)));
        var moduleTypes = asm.GetTypes().Where(t => typeof(ModuleInjectionHandler).IsAssignableFrom(t));

        foreach (var type in moduleTypes)
        {
            var moduleName = type.FullName;

            _logger.LogInformation("Injecting module: {ModuleName}", moduleName);

            if (Activator.CreateInstance(type) is ModuleInjectionHandler module)
            {
                module.AddModuleServices(_services, configuration);
                yield return new ModuleConfig
                {
                    Assembly = asm,
                    Handler = module,
                    Name = moduleName
                };
                continue;
            }

            throw new ApplicationException($"Failed to inject module: {moduleName}");
        }
    }
}