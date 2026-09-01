using System.IO;
using System.Threading.Tasks;
using Monody.AI.Tools.Capabilities.FetchUrl.ContentExtractor;
using Xunit;

namespace Monody.AI.Tools.Tests;

public class FetchUrlToolTests
{
    [Fact]
    public async Task ExtractMainContent_ReturnsArticleText()
    {
        var html = await File.ReadAllTextAsync("./TestData/HtmlArticle.html");

        var content = await HtmlContentExtractor.ExtractMainContentAsync(html);

        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task ExtractMainContent_IgnoresNavigationAndScripts()
    {
        var html = """
            <html><body>
              <nav><a href="/a">Home</a><a href="/b">About</a></nav>
              <script>var tracking = 1;</script>
              <article><p>
                The refactor kept every observable behaviour of the extractor intact.
                Sentences shorter than the cutoff are dropped from the extracted output.
                This paragraph exists purely so the article clears the length threshold.
              </p></article>
            </body></html>
            """;

        var content = await HtmlContentExtractor.ExtractMainContentAsync(html);

        Assert.Contains("refactor kept every observable behaviour", content);
        Assert.DoesNotContain("tracking", content);
        Assert.DoesNotContain("About", content);
    }
}
