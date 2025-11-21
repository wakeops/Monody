using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Monody.OpenAI.Tools.FetchBlueSky;

public sealed class BlueskyThreadResponse
{
    [JsonPropertyName("thread")]
    public ThreadViewPost Thread { get; set; }

    // Optional: capture error payloads
    [JsonPropertyName("error")]
    public string Error { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }
}

public sealed class ThreadViewPost
{
    [JsonPropertyName("post")]
    public BlueskyPost Post { get; set; } = default!;

    [JsonPropertyName("replies")]
    public List<ThreadViewPost> Replies { get; set; }
}

public sealed class BlueskyPost
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = default!;

    [JsonPropertyName("author")]
    public BlueskyAuthor Author { get; set; } = default!;

    [JsonPropertyName("record")]
    public BlueskyRecord Record { get; set; } = default!;
}

public sealed class BlueskyAuthor
{
    [JsonPropertyName("handle")]
    public string Handle { get; set; } = default!;
}

public sealed class BlueskyRecord
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
}
