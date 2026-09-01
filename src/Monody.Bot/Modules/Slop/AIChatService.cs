using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextToImage;
using Monody.AI.Agents;
using Monody.AI.SchemaJson;
using Monody.Bot.Modules.Slop.Models;
using Monody.Bot.Modules.Slop.Utils;
using OpenAI.Chat;
using SkChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace Monody.Bot.Modules.Slop;

public class AIChatService
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IChatCompletionService _chatService;
    private readonly ITextToImageService _imageService;
    private readonly Kernel _kernel;
    private readonly ConversationStore _conversationStore;

    public AIChatService(IChatCompletionService chatService, ITextToImageService imageService, Kernel kernel, ConversationStore conversationStore)
    {
        _chatService = chatService;
        _imageService = imageService;
        _kernel = kernel;
        _conversationStore = conversationStore;
    }

    public async Task<DiscordCompletionResponse> GetChatCompletionAsync(ulong interactionId, IGuild guild, IMessageChannel channel, IUser user, string prompt)
    {
        var conversation = GetOrCreateConversation(interactionId, guild, channel, user);

        conversation.History.AddUserMessage(new DiscordUserPrompt(user, prompt).ToString());

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                "discord_completion_response",
                BinaryData.FromString(StructuredOutputSchema.GenerateJsonSchema<DiscordCompletionResponse>()),
                jsonSchemaIsStrict: true)
        };

        var result = await _chatService.GetChatMessageContentsAsync(conversation.History, settings, _kernel);
        _conversationStore.Save(interactionId, conversation);

        var content = result.Last(m => m.Role == AuthorRole.Assistant).Content;
        return DeserializeFirstJsonObject(content);
    }

    public async Task<Uri> GetImageGenerationAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var url = await _imageService.GenerateImageAsync(prompt, 1024, 1024, cancellationToken: cancellationToken);
        return new Uri(url);
    }

    // When function calling and a strict JSON-schema response format are both active, the model
    // can append a second JSON object after the first (e.g. a repaired retry) in the same message.
    // Read only the first complete value instead of requiring the whole string to be one JSON document.
    private static DiscordCompletionResponse DeserializeFirstJsonObject(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        return JsonSerializer.Deserialize<DiscordCompletionResponse>(ref reader, _serializerOptions);
    }

    private DiscordConversation GetOrCreateConversation(ulong interactionId, IGuild guild, IMessageChannel channel, IUser user)
    {
        var conversation = _conversationStore.Get(interactionId);

        if (conversation == null)
        {
            var history = new ChatHistory();
            DiscordHelper.EnrichWithInteractionContext(history, interactionId, guild, channel);
            conversation = new DiscordConversation(interactionId, guild?.Id, channel?.Id, user.Id, history);
        }

        // Re-seed the system prompt so edits to it apply to conversations already in flight.
        foreach (var systemMessage in conversation.History.Where(m => m.Role == AuthorRole.System).ToList())
        {
            conversation.History.Remove(systemMessage);
        }

        conversation.History.Insert(0, new SkChatMessageContent(AuthorRole.System, SystemPrompt.Monody));

        return conversation;
    }
}
