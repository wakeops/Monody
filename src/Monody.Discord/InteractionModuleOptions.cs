using System.Reflection;

namespace Monody.Discord;

internal sealed class InteractionModuleOptions
{
    public List<Assembly> Assemblies { get; } = [];
}
