using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Monody.AI.Domain.Models;

namespace Monody.AI.Provider;

public interface IChatCompletionProvider
{
    string Name { get; }

    Task<ChatCompletionResult> CompleteAsync(List<ChatMessageDto> requestMessages, ChatConfiguration configuration = default, CancellationToken cancellationToken = default);

    Task<ImageGenerationResult> GenerateImageAsync(string prompt, CancellationToken cancellationToken = default);
}
