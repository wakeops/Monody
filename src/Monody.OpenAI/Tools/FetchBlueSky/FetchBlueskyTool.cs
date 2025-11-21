using System;
using System.Threading;
using System.Threading.Tasks;
using Monody.OpenAI.ToolHandler;

namespace Monody.OpenAI.Tools.FetchBlueSky;

internal class FetchBlueSkyTool : ToolHandler<FetchBlueSkyToolRequest, FetchBlueSkyToolResponse>, IToolHandler
{
    private const string _bskyHost = "bsky.app";

    private readonly BlueSkyService _blueSkyService;

    public FetchBlueSkyTool(BlueSkyService blueSkyService)
    {
        _blueSkyService = blueSkyService;
    }

    public override string Name => "fetch_bluesky";

    public override string Description => "Fetches the content of a given bsky URL for the assistant to analyze.";

    protected override async Task<FetchBlueSkyToolResponse> HandleAsync(FetchBlueSkyToolRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Url))
        {
            throw new ArgumentNullException(nameof(request.Url));
        }

        if (!IsBlueskyUrl(request.Url))
        {
            throw new ArgumentException("URL is not a valid bsky.app URL.", nameof(request.Url));
        }

        var content = await _blueSkyService.FetchThreadTextAsync(request.Url);

        return new FetchBlueSkyToolResponse
        {
            Content = content
        };
    }

    private static bool IsBlueskyUrl(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
           && uri.Host is _bskyHost;
}
