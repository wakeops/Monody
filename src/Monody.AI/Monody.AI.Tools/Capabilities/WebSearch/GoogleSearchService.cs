using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.CustomSearchAPI.v1;
using Google.Apis.CustomSearchAPI.v1.Data;
using Microsoft.Extensions.Options;

namespace Monody.AI.Tools.Capabilities.WebSearch;

internal class GoogleSearchService
{
    private readonly CustomSearchAPIService _searchApi;
    private readonly WebSearchToolOptions _options;

    public GoogleSearchService(CustomSearchAPIService searchApi, IOptions<WebSearchToolOptions> options)
    {
        _options = options.Value;
        _searchApi = searchApi;
    }

    public async Task<List<GoogleSearchResultItem>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var results = await ExecuteSearchAsync(query, CancellationToken.None);

        return [.. results.Select(item => new GoogleSearchResultItem
            {
                Title = item.Title,
                Link = item.Link,
                Snippet = item.Snippet
            })];
    }

    private async Task<IEnumerable<Result>> ExecuteSearchAsync(string query, CancellationToken cancellationToken)
    {
        var listRequest = _searchApi.Cse.List();
        listRequest.Cx = _options.GoogleSearchEngineId;
        listRequest.Q = query;

        var results = await listRequest.ExecuteAsync(cancellationToken);

        return results?.Items ?? [];
    }
}
