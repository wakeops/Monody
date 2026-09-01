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
using Monody.AI.Tools.Abstractions;
using Monody.Data;
using Monody.Data.Entities;
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
    private readonly IInvocationContext _invocationContext;

    public AIChatService(
        IChatCompletionService chatService,
        ITextToImageService imageService,
        Kernel kernel,
        ConversationStore conversationStore,
        IInvocationContext invocationContext)
    {
        _chatService = chatService;
        _imageService = imageService;
        _kernel = kernel;
        _conversationStore = conversationStore;
        _invocationContext = invocationContext;
    }

    public async Task<DiscordCompletionResponse> GetChatCompletionAsync(ulong interactionId, IGuild guild, IMessageChannel channel, IUser user, string prompt, CancellationToken cancellationToken = default)
    {
        var history = await LoadHistoryAsync(interactionId, guild, channel, cancellationToken);

        history.AddUserMessage(new DiscordUserPrompt(user, prompt).ToString());

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                "discord_completion_response",
                BinaryData.FromString(StructuredOutputSchema.GenerateJsonSchema<DiscordCompletionResponse>()),
                jsonSchemaIsStrict: true)
        };

        // Scopes the caller for the whole tool-calling loop, so per-user tools know who they
        // are acting for without the model being able to name someone else.
        using var scope = _invocationContext.BeginScope(user.Id, channel?.Id);

        var result = await _chatService.GetChatMessageContentsAsync(history, settings, _kernel, cancellationToken);

        // Persist before parsing: a malformed reply should not cost the user the whole thread.
        history.AddRange(result);
        await SaveHistoryAsync(interactionId, guild, channel, user, history, cancellationToken);

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

    private async Task<ChatHistory> LoadHistoryAsync(ulong interactionId, IGuild guild, IMessageChannel channel, CancellationToken cancellationToken)
    {
        var stored = await _conversationStore.GetTurnsAsync(interactionId, cancellationToken);

        var history = new ChatHistory();

        if (stored is null)
        {
            DiscordHelper.EnrichWithInteractionContext(history, interactionId, guild, channel);
        }
        else
        {
            foreach (var turn in stored)
            {
                history.Add(new SkChatMessageContent(new AuthorRole(turn.Role), turn.Content));
            }
        }

        // Seeded fresh every round, so edits to the prompt reach conversations already in flight.
        history.Insert(0, new SkChatMessageContent(AuthorRole.System, SystemPrompt.Monody));

        return history;
    }

    private Task SaveHistoryAsync(ulong interactionId, IGuild guild, IMessageChannel channel, IUser user, ChatHistory history, CancellationToken cancellationToken)
    {
        // Only the spoken turns are kept. Tool calls and their results were needed to finish this
        // round, not to carry the thread, and the system prompt is re-seeded on load.
        var turns = history
            .Where(m => m.Role == AuthorRole.User || m.Role == AuthorRole.Assistant)
            .Where(m => !string.IsNullOrWhiteSpace(m.Content))
            .Select(m => new ConversationTurn(m.Role.Label, m.Content));

        return _conversationStore.SaveAsync(interactionId, user.Id, channel?.Id, guild?.Id, turns, cancellationToken);
    }
}
