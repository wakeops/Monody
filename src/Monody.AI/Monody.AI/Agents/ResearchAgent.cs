using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Monody.AI.Tools.Abstractions;

namespace Monody.AI.Agents;

public class ResearchAgent : IResearchAgent
{
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
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var result = await _chatService.GetChatMessageContentsAsync(history, settings, _kernel, cancellationToken);
        return result.Last().Content?.Trim() ?? string.Empty;
    }
}
