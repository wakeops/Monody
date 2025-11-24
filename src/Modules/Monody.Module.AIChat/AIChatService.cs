using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Monody.Module.AIChat.Models;
using Monody.Module.AIChat.Utils;
using Monody.OpenAI.Services;
using OpenAI.Chat;
using OpenAI.Images;

namespace Monody.Module.AIChat;

public class AIChatService
{
    private readonly OpenAIService _openAIService;
    private readonly ConversationStore _conversationStore;

    public AIChatService(OpenAIService openAIService, ConversationStore conversationStore)
    {
        _openAIService = openAIService;
        _conversationStore = conversationStore;
    }

    public async Task<ChatCompletion> GetChatCompletionAsync(ulong interactionId, IGuild guild, IMessageChannel channel, IUser user, string prompt)
    {
        var conversation = GetOrCreateConversation(interactionId, guild, channel, user);

        var payload = new DiscordUserPrompt(user, prompt);

        conversation.Messages.Add(new UserChatMessage(payload.ToString()));

        var response = await _openAIService.GetChatCompletionAsync(conversation.Messages);

        _conversationStore.SaveConversation(interactionId.ToString(), conversation);

        return response;
    }

    public async Task<GeneratedImage> GetImageGenerationAsync(string prompt, GeneratedImageSize genSize)
    {
        return await _openAIService.GetImageGenerationAsync(prompt, genSize);
    }

    private DiscordConversation GetOrCreateConversation(ulong interactionId, IGuild guild, IMessageChannel channel, IUser user)
    {
        var conversation = _conversationStore.GetConversation(interactionId.ToString());
        if (conversation != null)
        {
            return conversation; 
        }

        var messages = new List<ChatMessage>();

        DiscordHelper.EnrichWithInteractionContext(messages, interactionId, guild, channel);

        return new DiscordConversation(interactionId.ToString(), guild?.Id, channel?.Id, user.Id, messages);
    } 
}
