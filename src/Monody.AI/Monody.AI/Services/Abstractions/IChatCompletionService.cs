using System.Threading;
using System.Threading.Tasks;
using Monody.AI.Domain.Models;

namespace Monody.AI.Services.Abstractions;

public interface IChatCompletionService
{
    Task<ChatCompletionResult> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default);
}
