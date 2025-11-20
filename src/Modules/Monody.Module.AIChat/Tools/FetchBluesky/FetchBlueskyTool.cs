using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Monody.Module.AIChat.Tools.FetchUrl;
using OpenAI.Chat;

namespace Monody.Module.AIChat.Tools.FetchBluesky;

internal class FetchBlueskyTool : IChatToolBase
{
    private readonly HttpClient _httpClient;

    public FetchBlueskyTool()
    {
        _httpClient = new HttpClient();
    }

    public string Name => "fetch_bluesky";

    public string SystemDescription => @"""
    You have access to a tool named fetch_bluesky that returns information about a bsky post.
    Whenever the user asks you to summarize, analyze, or read from a bsky.app URL, you should call fetch_bluesky with that URL before answering.
    """;

    public ChatTool Tool => ChatTool.CreateFunctionTool(Name,
        "Fetches the raw content of a given URL for the assistant to analyze.",
        BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "url": {
              "type": "string",
              "description": "The full https://bsky.app/profile/.../post/... URL of the post."
            }
          },
          "required": ["url"]
        }
        """)
    );

    public async Task<ToolChatMessage> ExecuteAsync(ChatToolCall toolFn)
    {
        var argsJson = toolFn.FunctionArguments;
        var args = JsonSerializer.Deserialize<FetchUrlToolRequest>(argsJson);

        if (args is null || string.IsNullOrWhiteSpace(args.Url))
        {
            // Defensive: if bad args, provide an error payload
            return ChatMessage.CreateToolMessage(toolFn.Id, "Error: Missing or invalid URL.");
        }

        var content = await FetchThreadTextAsync(args.Url);

        // Send the tool result back to the model
        return ChatMessage.CreateToolMessage(toolFn.Id, content);
    }

    private static bool IsBlueskyUrl(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
           && uri.Host is "bsky.app";

    public async Task<string> FetchThreadTextAsync(string bskyUrl)
    {
        if (!IsBlueskyUrl(bskyUrl))
        {
            throw new ArgumentException("URL is not a valid bsky.app URL.", nameof(bskyUrl));
        }

        var (handleOrDid, rkey) = ParseBlueskyPostUrl(bskyUrl);

        // 1. Resolve handle -> DID if necessary
        var did = handleOrDid.StartsWith("did:", StringComparison.OrdinalIgnoreCase)
            ? handleOrDid
            : await ResolveHandleToDidAsync(handleOrDid, CancellationToken.None);

        // 2. Build at:// URI and call getPostThread
        var atUri = $"at://{did}/app.bsky.feed.post/{rkey}";
        var threadUrl =
            $"https://public.api.bsky.app/xrpc/app.bsky.feed.getPostThread?uri={Uri.EscapeDataString(atUri)}&depth=10";

        using var resp = await _httpClient.GetAsync(threadUrl);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();

        var thread = JsonSerializer.Deserialize<BlueskyThreadResponse>(json)
                     ?? throw new InvalidOperationException("Failed to deserialize Bluesky thread response.");

        if (thread.Thread is null)
        {
            throw new InvalidOperationException("Thread is null in Bluesky response.");
        }

        // 3. Flatten thread into text
        var lines = new List<string>();
        CollectPosts(thread.Thread, lines);

        // Optional: trim if extremely long (token control)
        var combined = string.Join("\n\n", lines);
        const int maxChars = 20000;
        if (combined.Length > maxChars)
        {
            combined = combined[..maxChars] + "\n\n[Thread truncated]";
        }

        return combined;
    }

    private static (string handleOrDid, string rkey) ParseBlueskyPostUrl(string url)
    {
        var uri = new Uri(url);
        var segments = uri.AbsolutePath.Trim('/').Split('/');

        // Expected: /profile/{handleOrDid}/post/{rkey}
        if (segments.Length < 4 || segments[0] != "profile" || segments[2] != "post")
        {
            throw new ArgumentException("Not a Bluesky post URL of the form /profile/{handle}/post/{rkey}.", nameof(url));
        }

        var handleOrDid = segments[1];
        var rkey = segments[3];
        return (handleOrDid, rkey);
    }

    private async Task<string> ResolveHandleToDidAsync(string handle, CancellationToken ct)
    {
        var url =
            $"https://public.api.bsky.app/xrpc/com.atproto.identity.resolveHandle?handle={Uri.EscapeDataString(handle)}";

        using var resp = await _httpClient.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(json);
        var did = doc.RootElement.GetProperty("did").GetString();
        if (string.IsNullOrWhiteSpace(did))
        {
            throw new InvalidOperationException($"No DID returned when resolving handle '{handle}'.");
        }

        return did;
    }

    private static void CollectPosts(ThreadViewPost node, List<string> lines)
    {
        var author = node.Post?.Author?.Handle ?? "(unknown)";
        var text = node.Post?.Record?.Text ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(text))
        {
            lines.Add($"{author}: {text}");
        }

        if (node.Replies is { Count: > 0 })
        {
            foreach (var reply in node.Replies)
            {
                CollectPosts(reply, lines);
            }
        }
    }
}

public sealed class BlueskyThreadResponse
{
    [JsonPropertyName("thread")]
    public ThreadViewPost? Thread { get; set; }

    // Optional: capture error payloads
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public sealed class ThreadViewPost
{
    [JsonPropertyName("post")]
    public BlueskyPost Post { get; set; } = default!;

    [JsonPropertyName("replies")]
    public List<ThreadViewPost>? Replies { get; set; }
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
