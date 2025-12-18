using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord;
using Monody.AI.Domain.Models;
using Monody.AI.Provider;
using Monody.Bot.Modules.Slop.Models;
using Monody.Bot.Modules.Slop.Utils;

namespace Monody.Bot.Modules.Slop;

public class AIChatService
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IChatCompletionProvider _chatCompletionProvider;
    private readonly ConversationStore _conversationStore;

    public AIChatService(IChatCompletionProvider chatCompletionProvider, ConversationStore conversationStore)
    {
        _chatCompletionProvider = chatCompletionProvider;
        _conversationStore = conversationStore;
    }

    public async Task<DiscordCompletionResponse> GetChatCompletionAsync(ulong interactionId, IGuild guild, IMessageChannel channel, IUser user, string prompt)
    {
        var conversation = GetOrCreateConversation(interactionId, guild, channel, user);

        var payload = new DiscordUserPrompt(user, prompt);

        conversation.Messages.Add(new ChatMessageDto { Role = ChatRole.User, Content = payload.ToString() });

        var configuration = new ChatConfiguration
        {
            StructuredOutputType = typeof(DiscordCompletionResponse)
        };
        
        var response = await _chatCompletionProvider.CompleteAsync(conversation.Messages, configuration);

        _conversationStore.SaveConversation(interactionId.ToString(), conversation);

        var responseMessage = response.Messages.Last(m => m.Role == ChatRole.Assistant)?.Content;

        return JsonSerializer.Deserialize<DiscordCompletionResponse>(responseMessage, _serializerOptions);
    }

    public async Task<ImageGenerationResult> GetImageGenerationAsync(string prompt)
    {
        return await _chatCompletionProvider.GenerateImageAsync(prompt);
    }

    private DiscordConversation GetOrCreateConversation(ulong interactionId, IGuild guild, IMessageChannel channel, IUser user)
    {
        var conversation = _conversationStore.GetConversation(interactionId.ToString());
        if (conversation == null)
        {
            var messages = new List<ChatMessageDto>();

            DiscordHelper.EnrichWithInteractionContext(messages, interactionId, guild, channel);

            conversation = new DiscordConversation(interactionId.ToString(), guild?.Id, channel?.Id, user.Id, messages);
        }

        conversation.Messages.RemoveAll(msg => msg.Role == ChatRole.System);
        conversation.Messages.Insert(0, new ChatMessageDto { Role = ChatRole.System, Content = SystemPrompt.Monody });

        return conversation;
    } 
}
