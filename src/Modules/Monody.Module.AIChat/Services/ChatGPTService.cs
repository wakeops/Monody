using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Monody.Module.AIChat.Tools;
using Monody.Module.AIChat.Tools.FetchUrl;
using OpenAI.Chat;
using OpenAI.Images;

namespace Monody.Module.AIChat.Services;

public class ChatGPTService
{
    private readonly ChatClient _chatClient;
    private readonly ImageClient _imageClient;
    private readonly ILogger<ChatGPTService> _logger;

    private readonly List<IChatToolBase> _availableTools =
    [
        new FetchUrlTool()
    ];

    public ChatGPTService(ChatClient chatClient, ImageClient imageClient, ILogger<ChatGPTService> logger)
    {
        _chatClient = chatClient;
        _imageClient = imageClient;
        _logger = logger;
    }

    public async Task<ChatCompletion> GetChatCompletionAsync(List<ChatMessage> messages, string prompt)
    {
        _logger.LogInformation("New chat request: {Prompt}", prompt);

        var completionMessages = new List<ChatMessage> { new SystemChatMessage(GetSystemPrompt()) };
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
            Tools = {
                new FetchUrlTool().Tool
            },
            ToolChoice = ChatToolChoice.CreateAutoChoice(),
            AllowParallelToolCalls = true
        };

        try
        {
            return await ExecuteChatCompletionAsync(messages, options);
        }
        catch (Exception ex)
        {
            throw HandleError(ex);
        }
    }

    private string GetSystemPrompt()
    {
        return string.Join("\n\n", [SystemPrompt.Default, .._availableTools.Select(t => t.SystemDescription)]);
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

                    var tool = _availableTools.FirstOrDefault(t => t.Name == toolFn.FunctionName);
                    if (tool == null)
                    {
                        _logger.LogError("Unknown tool function: {FunctionName}", toolFn.FunctionName);
                        throw new InvalidOperationException("Unavailable tool requested");
                    }

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
