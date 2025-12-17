using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Monody.AI.Domain.Models;
using Monody.AI.Provider.Services.Abstractions;
using OpenAI.Chat;
using OpenAI.Images;

namespace Monody.AI.Provider.OpenAI.Services;

public class OpenAIProvider : IChatCompletionProvider
{
    private readonly IToolDispatcher _toolDispatcher;
    private readonly OpenAIService _openAIService;

    public string Name => "openai";

    public OpenAIProvider(IToolDispatcher toolDispatcher, OpenAIService openAIService)
    {
        _toolDispatcher = toolDispatcher;
        _openAIService = openAIService;
    }

    public async Task<ChatCompletionResult> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        var messages = request.Messages
            .Select(MapToOpenAiMessage)
            .ToList();

        var options = new ChatCompletionOptions
        {
            PresencePenalty = 0f,
            Temperature = (float?)request.Temperature ?? 0.7f,
            TopP = 1f,
            MaxOutputTokenCount = request.MaxOutputTokens ?? 1000,
            FrequencyPenalty = 0f,
            ToolChoice = ChatToolChoice.CreateAutoChoice(),
            AllowParallelToolCalls = true
        };

        if (request.EnableTools)
        {
            foreach (var tool in GetFunctionTools())
            {
                options.Tools.Add(tool);
            }
        }

        var completion = await _openAIService.GetChatCompletionAsync(messages, options, cancellationToken);

        return new ChatCompletionResult
        {
            ProviderName = Name,
            Model = _openAIService.ChatModel,
            Messages = [.. messages.Select(MapToDto)]
        };
    }

    public async Task<ImageGenerationResult> GenerateImageAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var completion = await _openAIService.GetImageGenerationAsync(prompt, GeneratedImageSize.W1024xH1024, cancellationToken);

        return new ImageGenerationResult
        {
            ImageUri = completion.ImageUri,
            ImageBytes = completion.ImageBytes
        };
    }

    private IEnumerable<ChatTool> GetFunctionTools()
    {
        foreach (var toolMetadata in _toolDispatcher.GetAllMetadata())
        {
            var toolJsonSchema = ToolJsonSchemaBuilder.FromParameters(toolMetadata.Parameters);
            var schemaJson = toolJsonSchema.RootElement.GetRawText();
            yield return ChatTool.CreateFunctionTool(toolMetadata.Name, toolMetadata.Description, BinaryData.FromString(schemaJson));
        }
    }

    private static ChatMessage MapToOpenAiMessage(ChatMessageDto dto)
    {
        return dto.Role switch
        {
            ChatRole.System => ChatMessage.CreateSystemMessage(dto.Content),
            ChatRole.User => ChatMessage.CreateUserMessage(dto.Content),
            ChatRole.Assistant => ChatMessage.CreateAssistantMessage(dto.Content),
            ChatRole.Tool => new ToolChatMessage(dto.ToolCallId ?? "", dto.Content),
            _ => ChatMessage.CreateUserMessage(dto.Content)
        };
    }

    private static ChatMessageDto MapToDto(ChatMessage message)
    {
        var content = ExtractTextContent(message);

        return message switch
        {
            SystemChatMessage s => new ChatMessageDto { Role = ChatRole.System, Content = content },
            UserChatMessage u => new ChatMessageDto { Role = ChatRole.User, Content = content },
            AssistantChatMessage a => new ChatMessageDto { Role = ChatRole.Assistant, Content = content },
            ToolChatMessage t => new ChatMessageDto { Role = ChatRole.Tool, Content = content, ToolCallId = t.ToolCallId },
            _ => new ChatMessageDto { Role = ChatRole.User, Content = content }
        };
    }

    private static string ExtractTextContent(ChatMessage message)
    {
        if (message.Content is null || message.Content.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        foreach (var part in message.Content)
        {
            if (!string.IsNullOrEmpty(part.Text))
            {
                if (sb.Length > 0)
                {
                    sb.Append('\n');
                }

                sb.Append(part.Text);
            }
        }

        return sb.ToString();
    }
}
