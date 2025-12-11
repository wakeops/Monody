using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Monody.AI.Tools.ToolHandler;

namespace Monody.AI.Tools.Capabilities.WebSearch;

internal class WebSearchTool : ToolHandler<WebSearchToolRequest, WebSearchToolResponse>
{
    private readonly GoogleSearchService _searchService;

    public WebSearchTool(GoogleSearchService searchService)
    {
        _searchService = searchService;
    }

    public override string Name => "web_search";

    public override string Description => "Searches Google for up-to-date information and returns a short list of results with titles, snippets, and URLs.";

    protected override async Task<WebSearchToolResponse> HandleAsync(WebSearchToolRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ArgumentNullException(nameof(request.Query));
        }

        var results = await _searchService.SearchAsync(request.Query, cancellationToken);

        var sb = new StringBuilder();
        foreach (var result in results)
        {
            sb.AppendLine($"Title: {result.Title}");
            sb.AppendLine($"Snippet: {result.Snippet}");
            sb.AppendLine($"URL: {result.Link}");
            sb.AppendLine();
        }

        return new WebSearchToolResponse
        {
            Results = sb.ToString().Trim()
        };
    }
}
