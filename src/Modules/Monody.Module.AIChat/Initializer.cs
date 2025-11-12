using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monody.Domain.Module;
using Monody.Module.AIChat.Services;
using OpenAI.Chat;
using OpenAI.Images;

namespace Monody.Module.AIChat;

public class Initializer : ModuleInitializer
{
    public override void AddModuleServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<OpenAIOptions>()
            .BindConfiguration("OpenAI")
            .ValidateDataAnnotations()
            .ValidateOnStart()
            .Configure(opts =>
            {
                (opts.ChatModel, opts.ImageModel) = (
                    string.IsNullOrWhiteSpace(opts.ChatModel) ? Constants.DefaultChatModel : opts.ChatModel,
                    string.IsNullOrWhiteSpace(opts.ImageModel) ? Constants.DefaultImageModel : opts.ImageModel
                );
            });

        services.AddSingleton<ChatClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
            return new ChatClient(model: opts.ChatModel, apiKey: opts.ApiKey);
        });

        services.AddSingleton<ImageClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
            return new ImageClient(model: opts.ImageModel, apiKey: opts.ApiKey);
        });

        services.AddTransient<ChatGPTService>();
    }
}
