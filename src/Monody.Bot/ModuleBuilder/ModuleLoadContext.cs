using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Monody.Bot.ModuleBuilder;

public sealed class ModuleLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    private readonly List<string> _sharedDependencyPrefixes =
    [
        "Discord."
    ];

    public ModuleLoadContext(string modulePath)
    {
        _resolver = new AssemblyDependencyResolver(modulePath);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        if (!_sharedDependencyPrefixes.Any(depPrefix => assemblyName.Name!.StartsWith(depPrefix, StringComparison.OrdinalIgnoreCase))
            && !Default.Assemblies.Any(x => assemblyName.Name!.Equals(x.GetName().Name, StringComparison.OrdinalIgnoreCase)))
        {
            var path = _resolver.ResolveAssemblyToPath(assemblyName);
            if (path != null)
            {
                return LoadFromAssemblyPath(path);
            }
        }
        
        return null; // fallback to Default context
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? nint.Zero : LoadUnmanagedDllFromPath(path);
    }
}

