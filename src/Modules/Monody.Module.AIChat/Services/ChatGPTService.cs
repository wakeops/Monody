using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using OpenAI.Images;

namespace Monody.Module.AIChat.Services;

public class ChatGPTService
{
    private readonly ChatClient _chatClient;
    private readonly ImageClient _imageClient;
    private readonly ILogger<ChatGPTService> _logger;

    private const string _systemPrompt = @"
You are Monody, a calm, precise assistant. You respond to prompts in the Discord messaging application and 
should respond to the best of your abilities. Do not remember anything between responses. All responses should 
be in a markdown format compatible with Discord. If a 'Context' block is provided, use it for extra 
understanding, but do not reveal the raw context unless explicitly asked.";

    public ChatGPTService(ChatClient chatClient, ImageClient imageClient, ILogger<ChatGPTService> logger)
    {
        _chatClient = chatClient;
        _imageClient = imageClient;
        _logger = logger;
    }

    public async Task<ChatCompletion> GetChatCompletionAsync(List<ChatMessage> messages, string prompt)
    {
        _logger.Log_ChatMessageRequest(prompt);

        var completionMessages = new List<ChatMessage> { new SystemChatMessage(_systemPrompt) };
        completionMessages.AddRange(messages);
        completionMessages.Add(new UserChatMessage(prompt));

        return await GetChatCompletionAsync(completionMessages);
    }

    public async Task<GeneratedImage> GetImageGenerationAsync(string prompt, GeneratedImageSize genSize)
    {
        _logger.Log_ImageGenerationRequest(prompt);

        var options = new ImageGenerationOptions
        {
            Size = genSize
        };

        try
        {
            return await _imageClient.GenerateImageAsync(prompt, options);
        }
        catch (ClientResultException ex)
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
            FrequencyPenalty = 0f
        };

        try
        {
            return await _chatClient.CompleteChatAsync(messages, options);
        }
        catch (ClientResultException ex)
        {
            throw HandleError(ex);
        }
    }

    private Exception HandleError(ClientResultException ex)
    {
        _logger.Log_ChatCompletionError(ex.Message, ex);

        var statusText = ex.Status.ToString() ?? string.Empty;

        // Check for 4xx client errors using a string-based check to be robust across status types
        if (statusText.StartsWith("4", StringComparison.Ordinal))
        {
            return new InvalidOperationException("Unable to complete request");
        }

        return new InvalidOperationException(ex.Message);
    }
}

internal static partial class ChatGPTServiceLogging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "New chat request: {Prompt}")]
    public static partial void Log_ChatMessageRequest(this ILogger logger, string prompt);

    [LoggerMessage(Level = LogLevel.Information, Message = "New image generation request: {Prompt}")]
    public static partial void Log_ImageGenerationRequest(this ILogger logger, string prompt);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get chat completion: {ErrorMessage}")]
    public static partial void Log_ChatCompletionError(this ILogger logger, string errorMessage, Exception ex);
}
