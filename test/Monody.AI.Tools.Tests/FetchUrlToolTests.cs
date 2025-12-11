using System.IO;
using System.Threading.Tasks;
using Monody.AI.Tools.Capabilities.FetchUrl.ContentExtractor;
using Xunit;

namespace Monody.AI.Tools.Tests;

public class FetchUrlToolTests
{
    [Fact]
    public async Task HtmlProcessor_ExtractMainContent_Content()
    {
        // Arrange
        var testFilePath = @"./TestData/HtmlArticle.html";
        var doc = File.ReadAllText(testFilePath);

        // Act
        var content = await HtmlContentExtractor.ExtractMainContentAsync(doc);

        // Assert
        Assert.NotNull(content);
    }
}