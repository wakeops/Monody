using System;
using System.Collections.Generic;

namespace Monody.AI.Domain.Models;

public class ChatConfiguration
{
    public float Temperature { get; set; } = 0.7f;

    public int MaxOutputTokens { get; set; } = 1000;

    public Type StructuredOutputType { get; set; } = null;

    // Optional: enable tools, RAG, etc.
    public bool EnableTools { get; init; } = true;

    // Optional bag for provider-specific knobs (top_p, etc.)
    public Dictionary<string, object> ProviderOptions { get; init; } = [];
}
