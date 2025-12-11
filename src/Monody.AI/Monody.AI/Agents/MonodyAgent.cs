using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Monody.AI.Domain.Abstractions;
using Monody.AI.Domain.Models;
using Monody.AI.Provider;

namespace Monody.AI.Agents;

public class MonodyAgent : IMonodyAgent
{
    private readonly IChatCompletionProvider _provider;

    public MonodyAgent(IChatCompletionProvider provider)
    {
        _provider = provider;
    }

    public async Task<ChatCompletionResult> GetResultAsync(IEnumerable<ChatMessageDto> promptMessages, CancellationToken cancellationToken)
    {
        List<ChatMessageDto> messages = [
            new ChatMessageDto { Role = ChatRole.System, Content = SystemPrompt.Monody },
            ..promptMessages
        ];

        var agentRequest = new ChatCompletionRequest
        {
            Messages = messages,
        };

        return await _provider.CompleteAsync(agentRequest, cancellationToken);
    }
}
