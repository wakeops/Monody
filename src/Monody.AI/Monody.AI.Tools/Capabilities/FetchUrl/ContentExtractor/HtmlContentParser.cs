using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace Monody.AI.Tools.Capabilities.FetchUrl.ContentExtractor;

/// <summary>
/// Fallback article extraction for pages SmartReader can't handle: strip the chrome, prefer
/// semantic tags, and otherwise pick the highest-scoring block of text.
/// </summary>
public static partial class HtmlContentParser
{
    private const string NoiseSelector = "script,style,noscript,svg,footer,nav,aside,form";

    private static readonly string[] _noiseClasses = ["article-meta", "article-footer", "article-header", "tags"];

    public static string ExtractMainContent(IHtmlDocument doc)
    {
        RemoveNoise(doc);

        var semantic = doc.QuerySelector("article") ?? doc.QuerySelector("main");
        if (semantic != null && IsUsable(semantic))
        {
            return CleanText(semantic.TextContent);
        }

        var best = doc.QuerySelectorAll("div,section,body")
            .Select(node => new ContentCandidate(node))
            .Where(c => c.Score > 0)
            .MaxBy(c => c.Score);

        return best == null ? string.Empty : CleanText(best.Node.TextContent);
    }

    [GeneratedRegex(@"\s+")]
    internal static partial Regex WhitespaceRun();

    private static void RemoveNoise(IHtmlDocument doc)
    {
        foreach (var node in doc.QuerySelectorAll(NoiseSelector).ToList())
        {
            node.Remove();
        }

        var byClass = doc.All
            .Where(e => e.GetAttribute("class") is string classes
                        && _noiseClasses.Any(c => classes.Contains(c, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var node in byClass)
        {
            node.Remove();
        }
    }

    private static bool IsUsable(IElement element) => CleanText(element.TextContent).Length > 200;

    /// <summary>Collapses whitespace and drops sentence fragments, one sentence per line.</summary>
    private static string CleanText(string text)
    {
        var normalized = WhitespaceRun().Replace(text, " ").Trim();

        var sentences = new StringBuilder();

        foreach (var sentence in normalized.Split(". "))
        {
            if (sentence.Length > 30)
            {
                sentences.AppendLine(sentence.Trim() + ".");
            }
        }

        return sentences.ToString().Trim();
    }
}
