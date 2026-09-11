using System.Net;
using Microsoft.Extensions.Options;
using Monody.AI.Tools.Capabilities.Graylog;
using Monody.Services.Graylog;
using Xunit;

namespace Monody.AI.Tools.Tests;

public class GraylogPluginTests
{
    [Fact]
    public async Task RefusesToSearchWhenNotConfigured()
    {
        using var handler = new StubHandler(_ => throw new InvalidOperationException("Should never call out."));
        var plugin = BuildPlugin(new GraylogOptions(), handler);

        var result = await plugin.SearchAsync(new SearchGraylogToolRequest { Query = "*" });

        Assert.False(result.Success);
        Assert.Contains("not configured", result.Reason);
    }

    [Fact]
    public async Task RefusesToListStreamsWhenNotConfigured()
    {
        using var handler = new StubHandler(_ => throw new InvalidOperationException("Should never call out."));
        var plugin = BuildPlugin(new GraylogOptions(), handler);

        var result = await plugin.ListStreamsAsync();

        Assert.False(result.Success);
        Assert.Contains("not configured", result.Reason);
    }

    [Fact]
    public async Task ListsOnlyEnabledStreams()
    {
        const string responseJson = """
            {
              "streams": [
                { "id": "1", "title": "API", "description": "API service logs", "disabled": false },
                { "id": "2", "title": "Retired", "description": "No longer collected", "disabled": true }
              ]
            }
            """;

        using var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseJson) });
        var plugin = BuildPlugin(new GraylogOptions { BaseUrl = "https://graylog.example.com", ApiKey = "test-token" }, handler);

        var result = await plugin.ListStreamsAsync();

        Assert.True(result.Success);
        var stream = Assert.Single(result.Streams);
        Assert.Equal("1", stream.Id);
        Assert.Equal("API", stream.Title);
        Assert.Equal("API service logs", stream.Description);
    }

    [Fact]
    public async Task ScopesSearchToTheGivenStream()
    {
        Uri requestedUri = null;

        using var handler = new StubHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"total_results":0,"messages":[]}""") };
        });
        var plugin = BuildPlugin(new GraylogOptions { BaseUrl = "https://graylog.example.com", ApiKey = "test-token" }, handler);

        await plugin.SearchAsync(new SearchGraylogToolRequest { Query = "*", StreamId = "abc123" });

        Assert.Contains("filter=streams%3Aabc123", requestedUri.Query);
    }

    [Fact]
    public async Task SearchesAndFormatsMessagesWhenConfigured()
    {
        const string responseJson = """
            {
              "total_results": 2,
              "messages": [
                { "message": { "timestamp": "2026-01-01T00:00:00.000Z", "source": "api", "message": "Started" } }
              ]
            }
            """;

        using var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseJson) });
        var plugin = BuildPlugin(new GraylogOptions { BaseUrl = "https://graylog.example.com", ApiKey = "test-token" }, handler);

        var result = await plugin.SearchAsync(new SearchGraylogToolRequest { Query = "*" });

        Assert.True(result.Success);
        Assert.Equal(2, result.TotalResults);
        Assert.Equal(["[2026-01-01T00:00:00.000Z] api: Started"], result.Messages);
    }

    // The HttpClient outlives this method (the plugin/service hold onto it for the test's
    // duration) and disposing it would also dispose the caller's still-in-use handler, so it is
    // deliberately left for the test process to clean up rather than tracked here.
#pragma warning disable CA2000
    private static GraylogPlugin BuildPlugin(GraylogOptions options, HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("https://graylog.example.com/api/") };
        var service = new GraylogService(httpClient, Options.Create(options));

        return new GraylogPlugin(service);
    }
#pragma warning restore CA2000

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
