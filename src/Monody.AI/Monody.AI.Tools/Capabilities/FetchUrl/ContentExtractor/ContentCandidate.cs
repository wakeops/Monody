using System;
using System.Text.RegularExpressions;
using AngleSharp.Dom;

namespace Monody.AI.Tools.Capabilities.FetchUrl.ContentExtractor;

internal class ContentCandidate
{
    public IElement Node { get; }
    public int Score { get; }

    public ContentCandidate(IElement node)
    {
        Node = node;
        Score = CalculateScore(node);
    }

    private static int CalculateScore(IElement node)
    {
        var text = node.TextContent ?? string.Empty;
        text = Regex.Replace(text, @"\s+", " ").Trim();

        if (text.Length < 200)
        {
            return 0;
        }

        int score = 0;

        // Favor length
        score += Math.Min(text.Length / 100, 50);

        // Favor paragraphs (".//p")
        var paragraphCount = node.QuerySelectorAll("p")?.Length ?? 0;
        score += paragraphCount * 5;

        // Penalize link-heavy blocks (".//a")
        var linkCount = node.QuerySelectorAll("a")?.Length ?? 0;
        if (linkCount > 0)
        {
            var linkDensity = (double)linkCount / Math.Max(1, paragraphCount);
            score -= (int)(linkDensity * 10);
        }

        // Bonus for typical content identifiers
        var classAttr = node.GetAttribute("class") ?? string.Empty;
        var idAttr = node.Id ?? string.Empty;

        var classId = (classAttr + " " + idAttr).ToLowerInvariant();

        if (classId.Contains("content") ||
            classId.Contains("article") ||
            classId.Contains("article-main") ||
            classId.Contains("article-body") ||
            classId.Contains("post") ||
            classId.Contains("entry"))
        {
            score += 20;
        }

        return score;
    }
}
