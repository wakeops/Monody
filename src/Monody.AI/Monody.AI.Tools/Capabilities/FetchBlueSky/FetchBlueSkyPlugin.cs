using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Monody.Services.BlueSky;

namespace Monody.AI.Tools.Capabilities.FetchBlueSky;

public sealed class FetchBlueSkyPlugin(BlueSkyService blueSkyService)
{
    private const string BskyHost = "bsky.app";

    [KernelFunction("fetch_bluesky")]
    [Description("Fetches the content of a given bsky URL for the assistant to analyze.")]
    public async Task<FetchBlueSkyToolResponse> FetchAsync(FetchBlueSkyToolRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Url);

        if (!IsBlueskyUrl(request.Url))
        {
            throw new ArgumentException("URL is not a valid bsky.app URL.", nameof(request.Url));
        }

        var content = await blueSkyService.FetchThreadTextAsync(request.Url);

        return new FetchBlueSkyToolResponse
        {
            Content = content
        };
    }

    private static bool IsBlueskyUrl(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
           && uri.Host is BskyHost;
}
