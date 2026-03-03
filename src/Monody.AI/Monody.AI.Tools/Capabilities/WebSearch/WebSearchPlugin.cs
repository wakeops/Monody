using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Monody.Services.WebSearch;

namespace Monody.AI.Tools.Capabilities.WebSearch;

public sealed class WebSearchPlugin(GoogleSearchService searchService)
{
    [KernelFunction("web_search")]
    [Description("Searches Google for up-to-date information and returns a short list of results with titles, snippets, and URLs.")]
    public async Task<WebSearchToolResponse> SearchAsync(WebSearchToolRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ArgumentNullException(nameof(request.Query));
        }

        var results = await searchService.SearchAsync(request.Query, cancellationToken);

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
