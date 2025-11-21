using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monody.Domain.Extensions;
using Monody.Domain.Module;
using Monody.OpenAI;

namespace Monody.Module.AIChat;

public class InjectionHandler : ModuleInjectionHandler
{
    public override void AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetRequiredOptions<OpenAIOptions>("Module:OpenAI");

        services.AddOpenAI(config =>
        {
            config.ApiKey = options.ApiKey;
            config.ChatModel = options.ChatModel;
            config.ImageModel = options.ImageModel;
        });

        services.AddSingleton<AIChatService>();
    }
}
