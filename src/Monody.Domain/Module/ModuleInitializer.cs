using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Monody.Domain.Module;

public abstract class ModuleInitializer
{
    public abstract void AddModuleServices(IServiceCollection services, IConfiguration configuration);
}
