using System;
using System.Linq;
using AngleSharp.Dom;

namespace Monody.AI.Tools.Capabilities.FetchUrl.ContentExtractor;

/// <summary>A block of the page, scored on how much it looks like the article body.</summary>
public class ContentCandidate
{
    // "article" also matches article-main/article-body, "post" matches blog-post, and so on.
    private static readonly string[] _contentMarkers = ["content", "article", "post", "entry"];

    public IElement Node { get; }

    public int Score { get; }

    public ContentCandidate(IElement node)
    {
        Node = node;
        Score = CalculateScore(node);
    }

    private static int CalculateScore(IElement node)
    {
        var text = HtmlContentParser.WhitespaceRun().Replace(node.TextContent ?? string.Empty, " ").Trim();

        if (text.Length < 200)
        {
            return 0;
        }

        var paragraphCount = node.QuerySelectorAll("p").Length;
        var linkCount = node.QuerySelectorAll("a").Length;

        // Longer, paragraph-heavy blocks are article-like; link-heavy ones are usually navigation.
        var score = Math.Min(text.Length / 100, 50) + paragraphCount * 5;

        if (linkCount > 0)
        {
            score -= (int)((double)linkCount / Math.Max(1, paragraphCount) * 10);
        }

        var classAndId = $"{node.GetAttribute("class")} {node.Id}".ToLowerInvariant();

        if (_contentMarkers.Any(marker => classAndId.Contains(marker, StringComparison.Ordinal)))
        {
            score += 20;
        }

        return score;
    }
}
