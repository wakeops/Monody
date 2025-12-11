using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Monody.AI.Tools.Capabilities.FetchUrl.ContentExtractor;
using Monody.AI.Tools.ToolHandler;

namespace Monody.AI.Tools.Capabilities.FetchUrl;

internal class FetchUrlTool : ToolHandler<FetchUrlToolRequest, FetchUrlToolResponse>
{
    private const int _maxBodyLength = 20_000;

    private readonly HttpClient _httpClient;

    public FetchUrlTool(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override string Name => "fetch_url";

    public override string Description => "Fetches a URL over HTTP(S) and returns the status code and body.";

    protected override async Task<FetchUrlToolResponse> HandleAsync(FetchUrlToolRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Url))
        {
            throw new ArgumentNullException(nameof(request.Url));
        }

        var result = await _httpClient.GetAsync(request.Url);

        var response = new FetchUrlToolResponse
        {
            StatusCode = (int)result.StatusCode
        };

        if (!result.IsSuccessStatusCode)
        {
            response.Body = result.ReasonPhrase;

            return response;
        }

        var resultHtml = await result.Content.ReadAsStringAsync();

        response.Body = await HtmlContentExtractor.ExtractMainContentAsync(resultHtml);

        // Truncate to avoid huge payloads
        if (response.Body.Length > _maxBodyLength)
        {
            response.Body = string.Concat(response.Body.AsSpan(0, _maxBodyLength), "\n\n[Truncated]");
        }

        return response;
    }
}
