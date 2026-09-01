using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Monody.AI.Tools.Capabilities.FetchUrl.ContentExtractor;

namespace Monody.AI.Tools.Capabilities.FetchUrl;

public sealed class FetchUrlPlugin(HttpClient httpClient)
{
    private const int MaxBodyLength = 20_000;

    [KernelFunction("fetch_url")]
    [Description("Fetches a URL over HTTP(S) and returns the status code and body.")]
    public async Task<FetchUrlToolResponse> FetchAsync(FetchUrlToolRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Url);

        var result = await httpClient.GetAsync(request.Url, cancellationToken);

        var response = new FetchUrlToolResponse
        {
            StatusCode = (int)result.StatusCode
        };

        if (!result.IsSuccessStatusCode)
        {
            response.Body = result.ReasonPhrase;
            return response;
        }

        var resultHtml = await result.Content.ReadAsStringAsync(cancellationToken);

        response.Body = await HtmlContentExtractor.ExtractMainContentAsync(resultHtml);

        if (response.Body.Length > MaxBodyLength)
        {
            response.Body = string.Concat(response.Body.AsSpan(0, MaxBodyLength), "\n\n[Truncated]");
        }

        return response;
    }
}
