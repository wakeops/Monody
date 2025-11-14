using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monody.Domain.Module;

namespace Monody.Bot;

internal static partial class ModuleLoader
{
    private const string _moduleAssemblyRoot = "Monody.Module.";
    private static readonly List<Assembly> _modules = [];

    public static IReadOnlyCollection<Assembly> GetModuleAssemblies() => _modules;

    public static void LoadModuleAssemblies(IServiceCollection services, IConfiguration configuration, string rootModulePath)
    {
        var logger = CreateLogger(services);

        var moduleAssemblyPaths = Directory.EnumerateFiles(rootModulePath, $"{_moduleAssemblyRoot}*.dll", SearchOption.AllDirectories);

        foreach (var asmPath in moduleAssemblyPaths)
        {
            LoadModuleAssembly(services, configuration, logger, asmPath);
        }
    }

    private static ILogger CreateLogger(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ModuleLoader));
    }

    private static void LoadModuleAssembly(IServiceCollection services, IConfiguration configuration, ILogger logger, string moduleAssemblyPath)
    {
        var mlc = new ModuleLoadContext(moduleAssemblyPath);
        var asm = mlc.LoadFromAssemblyName(new(Path.GetFileNameWithoutExtension(moduleAssemblyPath)));

        foreach (var type in asm.GetTypes().Where(t => typeof(ModuleInitializer).IsAssignableFrom(t)))
        {
            logger.LogInformation("Loading module: {ModuleName}", asm.GetName().Name);

            if (Activator.CreateInstance(type) is ModuleInitializer module)
            {
                module.AddModuleServices(services, configuration);
                continue;
            }

            throw new ApplicationException($"Failed to initialize module: {asm.GetName().Name}");
        }

        _modules.Add(asm);
    }
}