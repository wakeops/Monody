using System.ComponentModel;
using Microsoft.SemanticKernel;
using Monody.Services.Graylog;

namespace Monody.AI.Tools.Capabilities.Graylog;

public sealed class GraylogPlugin(GraylogService graylogService)
{
    private const int _defaultRangeSeconds = 300;
    private const int _defaultLimit = 20;
    private const int _maxLimit = 100;

    [KernelFunction("list_graylog_streams")]
    [Description(
        "Lists this server's Graylog streams (log categories/sources). Call this before " +
        "search_graylog to find the right StreamId to scope a search to, rather than searching " +
        "everything. Only available when a Graylog API key is configured for this bot - check the " +
        "response's Success field rather than assuming it ran.")]
    public async Task<ListGraylogStreamsToolResponse> ListStreamsAsync(CancellationToken cancellationToken = default)
    {
        if (!graylogService.IsConfigured)
        {
            return new ListGraylogStreamsToolResponse
            {
                Success = false,
                Reason = "Graylog is not configured for this bot; no API key is set."
            };
        }

        var streams = await graylogService.GetStreamsAsync(cancellationToken);

        return new ListGraylogStreamsToolResponse
        {
            Success = true,
            Streams = [.. streams.Select(s => new GraylogStreamSummary
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description
            })]
        };
    }

    [KernelFunction("search_graylog")]
    [Description(
        "Searches this server's Graylog logs using Graylog's query syntax over a relative time " +
        "range. Call list_graylog_streams first and pass its StreamId to scope the search, rather " +
        "than searching every stream. Be sure to pass a stream's Id field (never its Title) as StreamId " +
        "or you may get a 403 response. Only available when a Graylog API key is configured for this " +
        "bot - check the response's Success field rather than assuming it ran.")]
    public async Task<SearchGraylogToolResponse> SearchAsync(SearchGraylogToolRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        if (!graylogService.IsConfigured)
        {
            return new SearchGraylogToolResponse
            {
                Success = false,
                Reason = "Graylog is not configured for this bot; no API key is set."
            };
        }

        var rangeSeconds = request.RangeSeconds is > 0 ? request.RangeSeconds.Value : _defaultRangeSeconds;
        var limit = Math.Clamp(request.Limit ?? _defaultLimit, 1, _maxLimit);

        var result = await graylogService.SearchAsync(request.Query, rangeSeconds, limit, request.StreamId, cancellationToken);

        return new SearchGraylogToolResponse
        {
            Success = true,
            TotalResults = result.TotalResults,
            Messages = [.. result.Messages]
        };
    }
}
