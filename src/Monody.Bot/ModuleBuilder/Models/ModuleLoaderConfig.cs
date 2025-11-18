using System.Collections.Generic;

namespace Monody.Bot.ModuleBuilder.Models;

internal class ModuleLoaderConfig
{
    public IEnumerable<ModuleConfig> ModuleConfigs { get; set; }
}
