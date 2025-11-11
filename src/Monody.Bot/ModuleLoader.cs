using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Monody.Bot;

internal static class ModuleLoader
{
    public static void LoadModules()
    {
        var baseDir = AppContext.BaseDirectory;
        
        LoadAssembliesFromOutput(baseDir, name => name.StartsWith("Monody.Module.", StringComparison.OrdinalIgnoreCase));
    }

    private static void LoadAssembliesFromOutput(string directory, Func<string, bool> dllFilter)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(directory, "*.dll"))
        {
            var fileName = Path.GetFileName(path);
            if (!dllFilter(fileName))
            {
                continue;
            }

            Assembly asm = null;
            try
            {
                var an = AssemblyName.GetAssemblyName(path);
                if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == an.Name))
                {
                    continue;
                }

                asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            }
            catch { }
        }
    }
}
