using System;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html.Parser;
using SmartReader;

namespace Monody.AI.Tools.Capabilities.FetchUrl.ContentExtractor;

public static class HtmlContentExtractor
{
    private static readonly HtmlParser _parser = new(new HtmlParserOptions
    {
        IsScripting = false,
        IsKeepingSourceReferences = false
    });

    public static async Task<string> ExtractMainContentAsync(string html)
    {
        var doc = await _parser.ParseDocumentAsync(html);

        try
        {
            using var reader = new Reader("https://localhost/", doc);

            var article = await reader.GetArticleAsync();
            if (article != null && !string.IsNullOrWhiteSpace(article.TextContent))
            {
                return article.TextContent;
            }
        }
        catch (Exception)
        {
        }

        return HtmlContentParser.ExtractMainContent(doc);
    }
}
