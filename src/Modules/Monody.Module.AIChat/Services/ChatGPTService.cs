using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monody.Module.AIChat.Tools;
using OpenAI.Chat;
using OpenAI.Images;

namespace Monody.Module.AIChat.Services;

public class ChatGPTService
{
    private readonly ChatClient _chatClient;
    private readonly ImageClient _imageClient;
    private readonly ChatToolProvider _chatToolProvider;
    private readonly OpenAIOptions _options;
    private readonly ILogger _logger;

    private readonly string _systemPrompt;
    private readonly List<ChatTool> _chatTools;

    public ChatGPTService(ChatClient chatClient, ImageClient imageClient, ChatToolProvider chatToolProvider, IOptions<OpenAIOptions> options, ILogger<ChatGPTService> logger)
    {
        _chatClient = chatClient;
        _imageClient = imageClient;
        _chatToolProvider = chatToolProvider;
        _options = options.Value;
        _logger = logger;

        _chatTools = [.. GetChatTools()];
        _systemPrompt = GetSystemPrompt();
    }

    public async Task<ChatCompletion> GetChatCompletionAsync(List<ChatMessage> messages, string prompt)
    {
        _logger.LogInformation("New chat request: {Prompt}", prompt);

        var completionMessages = new List<ChatMessage> { new SystemChatMessage(_systemPrompt) };
        completionMessages.AddRange(messages);
        completionMessages.Add(new UserChatMessage(prompt));

        try
        {
            return await GetChatCompletionAsync(completionMessages);
        }
        catch (Exception ex)
        {
            throw HandleError(ex);
        }
    }

    public async Task<GeneratedImage> GetImageGenerationAsync(string prompt, GeneratedImageSize genSize)
    {
        _logger.LogInformation("New image generation request: {Prompt}", prompt);

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

    private async Task<ChatCompletion> GetChatCompletionAsync(List<ChatMessage> messages)
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

        _chatTools.ForEach(options.Tools.Add);

        try
        {
            return await ExecuteChatCompletionAsync(messages, options);
        }
        catch (Exception ex)
        {
            throw HandleError(ex);
        }
    }

    private IEnumerable<ChatTool> GetChatTools()
    {
        foreach (var toolName in _options.Tools)
        {
            var tool = _chatToolProvider.GetChatTool(toolName);
            yield return tool.GetFunctionTool();
        }
    }

    private string GetSystemPrompt()
    {
        var sb = new StringBuilder(SystemPrompt.Default);

        foreach (var toolName in _options.Tools)
        {
            var tool = _chatToolProvider.GetChatTool(toolName);
            sb.Append("\n\n");
            sb.Append(tool);
        }

        return sb.ToString();
    }

    private async Task<ChatCompletion> ExecuteChatCompletionAsync(List<ChatMessage> messages, ChatCompletionOptions options)
    {
        var response = await _chatClient.CompleteChatAsync(messages, options);
        var content = response.Value;

        switch(content.FinishReason)
        {
            case ChatFinishReason.ToolCalls:
                messages.Add(new AssistantChatMessage(content));

                foreach (var toolCall in content.ToolCalls)
                {
                    if (toolCall is not ChatToolCall toolFn)
                    {
                        continue;
                    }

                    var tool = _chatToolProvider.GetChatTool(toolCall.FunctionName)
                        ?? throw new InvalidOperationException("Unavailable tool requested");

                    _logger.LogInformation("Executing tool function: {FunctionName}", toolFn.FunctionName);

                    var message = await tool.ExecuteAsync(toolFn);
                    messages.Add(message);
                }

                return await ExecuteChatCompletionAsync(messages, options);
            default:
                return content;
        }
    }

    private InvalidOperationException HandleError(Exception ex)
    {
        _logger.LogError(ex, "Failed to get chat completion: {ErrorMessage}", ex.Message);

        if (ex is ClientResultException cex)
        {
            var statusText = cex.Status.ToString() ?? string.Empty;

            // Check for 4xx client errors
            if (statusText.StartsWith("4", StringComparison.Ordinal))
            {
                return new InvalidOperationException("Unable to complete request");
            }
        }

        return new InvalidOperationException(ex.Message);
    }
}
