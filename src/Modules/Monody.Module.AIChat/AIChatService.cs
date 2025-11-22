using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Monody.Module.AIChat.Utils;
using Monody.OpenAI.Services;
using OpenAI.Chat;
using OpenAI.Images;

namespace Monody.Module.AIChat;

public class AIChatService
{
    private readonly OpenAIService _openAIService;

    public AIChatService(OpenAIService openAIService)
    {
        _openAIService = openAIService;
    }

    public async Task<ChatCompletion> GetChatCompletionAsync(ulong interactionId, IGuild guild, IMessageChannel channel, IUser user, string prompt, int lookbackCount)
    {
        var messages = new List<ChatMessage>();

        DiscordHelper.EnrichWithInteractionContext(messages, interactionId, guild, channel, user);

        await DiscordHelper.EnrichWithMessageHistoryAsync(messages, channel, lookbackCount);

        messages.Add(new UserChatMessage(prompt));

        return await _openAIService.GetChatCompletionAsync(messages);
    }

    public async Task<GeneratedImage> GetImageGenerationAsync(string prompt, GeneratedImageSize genSize)
    {
        return await _openAIService.GetImageGenerationAsync(prompt, genSize);
    }
}
