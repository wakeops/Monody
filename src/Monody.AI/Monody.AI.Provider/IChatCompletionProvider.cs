using System.Threading;
using System.Threading.Tasks;
using Monody.AI.Domain.Models;

namespace Monody.AI.Provider;

public interface IChatCompletionProvider
{
    string Name { get; }

    Task<ChatCompletionResult> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default);

    Task<ImageGenerationResult> GenerateImageAsync(string prompt, CancellationToken cancellationToken = default);
}
