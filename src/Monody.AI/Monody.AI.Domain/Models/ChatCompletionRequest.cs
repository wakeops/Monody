using System.Collections.Generic;

namespace Monody.AI.Domain.Models;

public sealed class ChatCompletionRequest
{
    public List<ChatMessageDto> Messages { get; init; } = [];

    public double? Temperature { get; init; }

    public int? MaxOutputTokens { get; init; }

    // Optional: enable tools, RAG, etc.
    public bool EnableTools { get; init; } = true;

    // Optional bag for provider-specific knobs (top_p, etc.)
    public Dictionary<string, object> ProviderOptions { get; init; } = [];
}
