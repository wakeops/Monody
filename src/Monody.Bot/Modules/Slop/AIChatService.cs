using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Monody.AI.Domain.Abstractions;
using Monody.AI.Domain.Models;
using Monody.AI.Provider;
using Monody.Bot.Modules.Slop.Models;
using Monody.Bot.Modules.Slop.Utils;

namespace Monody.Bot.Modules.Slop;

public class AIChatService
{
    private readonly IMonodyAgent _monodyAgent;
    private readonly IChatCompletionProvider _chatCompletionProvider;
    private readonly ConversationStore _conversationStore;

    public AIChatService(IMonodyAgent monodyAgent, IChatCompletionProvider chatCompletionProvider, ConversationStore conversationStore)
    {
        _monodyAgent = monodyAgent;
        _chatCompletionProvider = chatCompletionProvider;
        _conversationStore = conversationStore;
    }

    public async Task<ChatCompletionResult> GetChatCompletionAsync(ulong interactionId, IGuild guild, IMessageChannel channel, IUser user, string prompt)
    {
        var conversation = GetOrCreateConversation(interactionId, guild, channel, user);

        var payload = new DiscordUserPrompt(user, prompt);

        conversation.Messages.Add(new ChatMessageDto { Role = ChatRole.User, Content = payload.ToString() });

        var response = await _monodyAgent.GetResultAsync(conversation.Messages);

        _conversationStore.SaveConversation(interactionId.ToString(), conversation);

        return response;
    }

    public async Task<ImageGenerationResult> GetImageGenerationAsync(string prompt)
    {
        return await _chatCompletionProvider.GenerateImageAsync(prompt);
    }

    private DiscordConversation GetOrCreateConversation(ulong interactionId, IGuild guild, IMessageChannel channel, IUser user)
    {
        var conversation = _conversationStore.GetConversation(interactionId.ToString());
        if (conversation != null)
        {
            return conversation; 
        }

        var messages = new List<ChatMessageDto>();

        DiscordHelper.EnrichWithInteractionContext(messages, interactionId, guild, channel);

        return new DiscordConversation(interactionId.ToString(), guild?.Id, channel?.Id, user.Id, messages);
    } 
}
