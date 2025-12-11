using System.Threading;
using System.Threading.Tasks;
using Monody.AI.Domain.Models;
using Monody.AI.Provider;
using Monody.AI.Services.Abstractions;

namespace Monody.AI.Services;

internal sealed class ChatCompletionService : IChatCompletionService
{
    private readonly IChatCompletionProvider _provider;

    public ChatCompletionService(IChatCompletionProvider provider)
    {
        _provider = provider;
    }

    public Task<ChatCompletionResult> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        return _provider.CompleteAsync(request, cancellationToken);
    }
}
