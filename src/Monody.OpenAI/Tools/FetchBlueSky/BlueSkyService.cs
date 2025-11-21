using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Monody.OpenAI.Tools.FetchBlueSky;

internal class BlueSkyService
{
    private readonly HttpClient _httpClient;

    public BlueSkyService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> FetchThreadTextAsync(string bskyUrl)
    {
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
