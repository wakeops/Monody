using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextToImage;
using Monody.AI.SchemaJson;
using Monody.Bot.Modules.Slop.Models;
using Monody.Bot.Modules.Slop.Utils;
using OpenAI.Chat;
using SkChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace Monody.Bot.Modules.Slop;

public class AIChatService
{
    private readonly JsonSerializerOptions _serializerOptions = new()
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

        var payload = new DiscordUserPrompt(user, prompt);
        conversation.History.AddUserMessage(payload.ToString());

        var schemaJson = StructuredOutputSchema.GenerateJsonSchema<DiscordCompletionResponse>();

        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                "discord_completion_response",
                BinaryData.FromString(schemaJson),
                jsonSchemaIsStrict: true)
        };

        var result = await _chatService.GetChatMessageContentsAsync(conversation.History, settings, _kernel);
        _conversationStore.SaveConversation(interactionId.ToString(), conversation);

        var responseContent = result.Last(m => m.Role == AuthorRole.Assistant).Content;
        return JsonSerializer.Deserialize<DiscordCompletionResponse>(responseContent, _serializerOptions);
    }

    public async Task<System.Uri> GetImageGenerationAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var url = await _imageService.GenerateImageAsync(prompt, 1024, 1024, cancellationToken: cancellationToken);
        return new System.Uri(url);
    }

    private DiscordConversation GetOrCreateConversation(ulong interactionId, IGuild guild, IMessageChannel channel, IUser user)
    {
        var conversation = _conversationStore.GetConversation(interactionId.ToString());
        if (conversation == null)
        {
            var history = new ChatHistory();
            DiscordHelper.EnrichWithInteractionContext(history, interactionId, guild, channel);
            conversation = new DiscordConversation(interactionId.ToString(), guild?.Id, channel?.Id, user.Id, history);
        }

        // Replace system message: remove existing ones, then insert at front
        for (int i = conversation.History.Count - 1; i >= 0; i--)
        {
            if (conversation.History[i].Role == AuthorRole.System)
            {
                conversation.History.RemoveAt(i);
            }

        }
        conversation.History.Insert(0, new SkChatMessageContent(AuthorRole.System, SystemPrompt.Monody));

        return conversation;
    }
}
