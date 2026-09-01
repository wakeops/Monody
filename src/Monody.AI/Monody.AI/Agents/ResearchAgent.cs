using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Monody.AI.Tools.Abstractions;

namespace Monody.AI.Agents;

/// <summary>
/// A sub-agent that runs the noisy search-and-read loop in its own context, so only the
/// distilled answer reaches the conversation the user is actually having.
/// </summary>
public class ResearchAgent : IResearchAgent
{
    /// <summary>
    /// The only tools the sub-agent may use. This is what keeps it a research agent rather than
    /// a second copy of the main assistant - and it excludes research_assistant, which otherwise
    /// lets the sub-agent call itself with nothing bounding the recursion.
    /// </summary>
    private static readonly string[] _allowedTools = ["web_search", "fetch_url", "fetch_bluesky", "current_time"];

    /// <summary>The parent is waiting inside a Discord interaction, so the loop cannot run forever.</summary>
    private static readonly TimeSpan _budget = TimeSpan.FromMinutes(2);

    private readonly IChatCompletionService _chatService;
    private readonly Kernel _kernel;

    public ResearchAgent(IChatCompletionService chatService, Kernel kernel)
    {
        _chatService = chatService;
        _kernel = kernel;
    }

    public async Task<string> GetResultAsync(string prompt, CancellationToken cancellationToken)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(SystemPrompt.ResearchAgent);
        history.AddUserMessage(prompt);

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(GetAllowedFunctions())
        };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_budget);

        try
        {
            var result = await _chatService.GetChatMessageContentsAsync(history, settings, _kernel, timeout.Token);

            // Match the main chat path: the final assistant turn is the answer, and the last
            // message is not necessarily it.
            return result.LastOrDefault(m => m.Role == AuthorRole.Assistant)?.Content?.Trim() ?? string.Empty;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return "The research agent ran out of time before it finished. Answer from what you already know, and say the lookup was incomplete.";
        }
    }

    private IReadOnlyList<KernelFunction> GetAllowedFunctions() =>
        [.. _kernel.Plugins
            .SelectMany(plugin => plugin)
            .Where(function => _allowedTools.Contains(function.Name, StringComparer.Ordinal))];
}
