using System.Reflection;
using Monody.Bot.Modules;

namespace Monody.Bot.ModuleBuilder.Models;

internal class ModuleConfig
{
    public Assembly Assembly { get; init; }

    public ModuleInjectionHandler Handler { get; init; }

    public string Name { get; init; }
}
