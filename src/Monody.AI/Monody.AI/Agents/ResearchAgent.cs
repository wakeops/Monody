using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Monody.AI.Domain.Abstractions;
using Monody.AI.Domain.Models;
using Monody.AI.Provider;

namespace Monody.AI.Agents;

public class ResearchAgent : IResearchAgent
{
    private readonly IChatCompletionProvider _provider;

    public ResearchAgent(IChatCompletionProvider provider)
    {
        _provider = provider;
    }

    public async Task<string> GetResultAsync(string prompt, CancellationToken cancellationToken)
    {
        List<ChatMessageDto> messages = [
            new ChatMessageDto { Role = ChatRole.System, Content = SystemPrompt.ResearchAgent },
            new ChatMessageDto { Role = ChatRole.User, Content = prompt }
        ];

        var agentRequest = new ChatCompletionRequest
        {
            Messages = messages
        };

        var completion = await _provider.CompleteAsync(agentRequest, cancellationToken);

        return completion.Messages.Last().Content.Trim();
    }
}
