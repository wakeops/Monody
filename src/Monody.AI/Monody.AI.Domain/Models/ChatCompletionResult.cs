using System.Collections.Generic;

namespace Monody.AI.Domain.Models;

public sealed class ChatCompletionResult
{
    public string ProviderName { get; init; }

    public string Model { get; init; }

    public List<ChatMessageDto> Messages { get; init; } = [];
}
