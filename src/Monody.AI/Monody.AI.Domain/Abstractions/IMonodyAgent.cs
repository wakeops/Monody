using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Monody.AI.Domain.Models;

namespace Monody.AI.Domain.Abstractions;

public interface IMonodyAgent
{
    Task<ChatCompletionResult> GetResultAsync(IEnumerable<ChatMessageDto> promptMessages, CancellationToken cancellationToken = default);
}
