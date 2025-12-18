using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monody.AI;

namespace Monody.Bot.Modules.Slop;

public class InjectionHandler : ModuleInjectionHandler
{
    public override void AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMonodyAI(configuration);

        services.AddSingleton<ConversationStore>();
        services.AddSingleton<AIChatService>();
    }
}
