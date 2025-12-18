using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Monody.Bot.Modules;

/// <summary>
/// Any assemblies that contain Discord modules must implement a class derived from this abstract base class.
/// </summary>
public abstract class ModuleInjectionHandler
{
    public abstract void AddModuleServices(IServiceCollection services, IConfiguration configuration);
}
