using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Monody.OpenAI.Exceptions;
using OpenAI.Chat;
using OpenAI.Images;

namespace Monody.OpenAI.Services;

public class OpenAIService
{
    private readonly ChatClient _chatClient;
    private readonly ImageClient _imageClient;
    private readonly ToolDispatcher _toolDispatcher;
    private readonly ILogger _logger;

    public OpenAIService(ChatClient chatClient, ImageClient imageClient, ToolDispatcher toolDispatcher, ILogger<OpenAIService> logger)
    {
        _chatClient = chatClient;
        _imageClient = imageClient;
        _toolDispatcher = toolDispatcher;
        _logger = logger;
    }

    public async Task<ChatCompletion> GetChatCompletionAsync(List<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        var options = new ChatCompletionOptions
        {
            PresencePenalty = 0f,
            Temperature = 0.7f,
            TopP = 1f,
            MaxOutputTokenCount = 1000,
            FrequencyPenalty = 0f,
            ToolChoice = ChatToolChoice.CreateAutoChoice(),
            AllowParallelToolCalls = true
        };

        foreach (var tool in GetFunctionTools())
        {
            options.Tools.Add(tool);
        }

        try
        {
            var inProcessMessages = new List<ChatMessage>();

            ChatCompletion content;

            do
            {
                List<ChatMessage> completionMessages = [SystemPrompt.Default, .. messages, .. inProcessMessages];

                var response = await _chatClient.CompleteChatAsync(completionMessages, options, cancellationToken);
                content = response.Value;

                inProcessMessages.Add(new AssistantChatMessage(content));

                switch (content.FinishReason)
                {
                    case ChatFinishReason.ToolCalls:

                        var toolResults = await ProcessToolsAsync(content.ToolCalls, cancellationToken);
                        inProcessMessages.AddRange(toolResults);
                        break;
                }
            } while (content.FinishReason != ChatFinishReason.Stop);

            messages.AddRange(inProcessMessages);

            return content;
        }
        catch (Exception ex)
        {
            throw HandleError(ex);
        }
    }

    public async Task<GeneratedImage> GetImageGenerationAsync(string prompt, GeneratedImageSize genSize, CancellationToken cancellationToken = default)
    {
        var options = new ImageGenerationOptions
        {
            Size = genSize
        };

        try
        {
            return await _imageClient.GenerateImageAsync(prompt, options);
        }
        catch (Exception ex)
        {
            throw HandleError(ex);
        }
    }

    private IEnumerable<ChatTool> GetFunctionTools()
    {
        foreach (var toolMetadata in _toolDispatcher.GetAllMetadata())
        {
            var schemaJson = toolMetadata.ParametersSchema.RootElement.GetRawText();
            yield return ChatTool.CreateFunctionTool(toolMetadata.Name, toolMetadata.Description, BinaryData.FromString(schemaJson));
        }
    }

    private async Task<IEnumerable<ToolChatMessage>> ProcessToolsAsync(IReadOnlyList<ChatToolCall> toolCalls, CancellationToken cancellationToken)
    {
        var toolTasks = new List<Task<ToolChatMessage>>();

        foreach (var toolCall in toolCalls)
        {
            var toolTask = HandleToolCallAsync(toolCall, cancellationToken);

            toolTasks.Add(toolTask);
        }

        return await Task.WhenAll(toolTasks);
    }

    private async Task<ToolChatMessage> HandleToolCallAsync(ChatToolCall toolCall, CancellationToken cancellationToken)
    {
        string result;

        try
        {
            result = await _toolDispatcher.ExecuteAsync(toolCall.FunctionName, toolCall.FunctionArguments, cancellationToken);
        }
        catch (ToolException tex)
        {
            var toolError = new
            {
                toolFailureException = tex.InnerException.GetType().Name,
                toolFailureReason = tex.Message
            };

            result = JsonSerializer.Serialize(toolError);
        }

        return ChatMessage.CreateToolMessage(toolCall.Id, result);
    }

    private Exception HandleError(Exception ex)
    {
        if (ex is ClientResultException)
        {
            _logger.LogError(ex, "Open AI client call failed");

            return new OpenAIServerException(ex);
        }

        _logger.LogError(ex, "Failed to get chat completion: {ErrorMessage}", ex.Message);

        return new ApplicationException(ex.Message, ex);
    }
}
