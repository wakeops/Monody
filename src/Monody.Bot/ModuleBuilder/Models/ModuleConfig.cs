using System.Reflection;
using Monody.Domain.Module;

namespace Monody.Bot.ModuleBuilder.Models;

internal class ModuleConfig
{
    public Assembly Assembly { get; init; }

    public ModuleInjectionHandler Handler { get; init; }

    public string Name { get; init; }
}
