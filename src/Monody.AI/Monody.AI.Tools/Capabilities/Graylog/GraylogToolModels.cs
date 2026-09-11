using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Monody.AI.Tools.Capabilities.Graylog;

public sealed class SearchGraylogToolRequest
{
    [Description(
        "The Graylog query string, using Graylog's Lucene-like query syntax, e.g. " +
        "'error AND service:api' or 'level:3'. Use '*' to match every message in the time range.")]
    [Required]
    public string Query { get; set; }

    [Description("How far back to search, in seconds from now. Defaults to 300 (5 minutes) when omitted.")]
    public int? RangeSeconds { get; set; }

    [Description("Maximum number of messages to return, newest first. Defaults to 20, capped at 100.")]
    public int? Limit { get; set; }

    [Description(
        "The Id of one stream to search, from list_graylog_streams. Omit to search across every " +
        "stream, which is noisier and slower - prefer scoping to a stream once you know which one is relevant.")]
    public string StreamId { get; set; }
}

public sealed class SearchGraylogToolResponse
{
    [Description("Whether the search actually ran. False when Graylog isn't configured for this bot, or the request failed.")]
    public bool Success { get; set; }

    [Description("Why the search did not run, when Success is false.")]
    public string Reason { get; set; }

    [Description("Total number of matching messages in Graylog - may be more than were returned.")]
    public int TotalResults { get; set; }

    [Description("Up to Limit matching log messages, newest first, formatted as '[timestamp] source: message'.")]
    public List<string> Messages { get; set; } = [];
}

public sealed class ListGraylogStreamsToolResponse
{
    [Description("Whether the list actually ran. False when Graylog isn't configured for this bot, or the request failed.")]
    public bool Success { get; set; }

    [Description("Why the list did not run, when Success is false.")]
    public string Reason { get; set; }

    [Description("Every enabled stream this bot can search, with the Id to pass as search_graylog's StreamId.")]
    public List<GraylogStreamSummary> Streams { get; set; } = [];
}

public sealed class GraylogStreamSummary
{
    [Description("Pass this as search_graylog's StreamId to scope a search to this stream.")]
    public string Id { get; set; }

    [Description("The stream's display name.")]
    public string Title { get; set; }

    [Description("What this stream collects, if the stream has a description.")]
    public string Description { get; set; }
}
