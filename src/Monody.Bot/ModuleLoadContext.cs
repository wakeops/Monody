using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Monody.Bot;

public sealed class ModuleLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public ModuleLoadContext(string modulePath)
    {
        _resolver = new AssemblyDependencyResolver(modulePath);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name!.StartsWith("Discord.", StringComparison.OrdinalIgnoreCase)
            || Default.Assemblies.Any(x => assemblyName.Name!.Equals(x.GetName().Name, StringComparison.OrdinalIgnoreCase)))
        {
            return null; // fallback to Default context
        }

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? nint.Zero : LoadUnmanagedDllFromPath(path);
    }
}

