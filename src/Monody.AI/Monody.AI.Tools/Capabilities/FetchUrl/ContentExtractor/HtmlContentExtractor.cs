using System;
using System.Threading.Tasks;
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
        var article = await TryReadArticleAsync(html);
        if (!string.IsNullOrWhiteSpace(article))
        {
            return article;
        }

        // Parsed fresh: disposing the Reader above also tears down the document it was given.
        return HtmlContentParser.ExtractMainContent(await _parser.ParseDocumentAsync(html));
    }

    private static async Task<string> TryReadArticleAsync(string html)
    {
        try
        {
            // SmartReader needs a base URI to resolve relative links; the text it returns
            // doesn't depend on it, so a placeholder is fine.
            using var reader = new Reader("https://localhost/", await _parser.ParseDocumentAsync(html));

            return (await reader.GetArticleAsync())?.TextContent;
        }
        catch (Exception)
        {
            // SmartReader throws on pages it can't make sense of; fall back to our own parser.
            return null;
        }
    }
}
