using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace Monody.AI.Tools.Capabilities.FetchUrl.ContentExtractor;

public static class HtmlContentParser
{
    public static string ExtractMainContent(IHtmlDocument doc)
    {
        // Remove obvious noise
        RemoveNodes(doc, "script,style,noscript,svg,footer,nav,aside,form");

        RemoveNodesByClass(doc, "article-meta", "article-footer", "article-header", "tags");

        // Prefer semantic tags
        var article = doc.QuerySelector("article");
        if (IsUsable(article))
        {
            return CleanText(article.TextContent);
        }

        var main = doc.QuerySelector("main");
        if (IsUsable(main))
        {
            return CleanText(main.TextContent);
        }

        // Score candidates
        var candidates = doc
            .QuerySelectorAll("div,section,body")
            .OfType<IElement>()
            .Select(node => new ContentCandidate(node))
            .Where(c => c.Score > 0)
            .OrderByDescending(c => c.Score)
            .ToList();

        if (candidates == null || candidates.Count == 0)
        {
            return string.Empty;
        }

        return CleanText(candidates.First().Node.TextContent);
    }

    private static bool IsUsable(IElement element)
    {
        if (element is null)
        {
            return false;
        }

        var text = CleanText(element.TextContent);
        return text.Length > 200;
    }

    private static void RemoveNodes(IHtmlDocument doc, string cssSelector)
    {
        var nodes = doc.QuerySelectorAll(cssSelector);
        if (nodes == null || nodes.Length == 0)
        {
            return;
        }

        foreach (var node in nodes.ToList())
        {
            node.Remove(); // IChildNode.Remove() extension
        }
    }

    private static void RemoveNodesByClass(IHtmlDocument doc, params string[] classNames)
    {
        // Equivalent to doc.DocumentNode.Descendants() in HtmlAgilityPack:
        // `doc.All` gives all elements in the document.
        var nodes = doc.All
            .OfType<IElement>()
            .Where(e =>
            {
                var classAttr = e.GetAttribute("class");
                if (classAttr == null)
                {
                    return false;
                }

                return classNames.Any(c =>
                    classAttr.Contains(c, StringComparison.OrdinalIgnoreCase));
            })
            .ToList();

        foreach (var node in nodes)
        {
            node.Remove();
        }
    }

    // Normalize whitespace while preserving paragraphs
    private static string CleanText(string text)
    {
        var normalized = Regex.Replace(text, @"\s+", " ").Trim();

        var sb = new StringBuilder();

        foreach (var paragraph in normalized.Split(". "))
        {
            if (paragraph.Length > 30)
            {
                sb.AppendLine(paragraph.Trim() + ".");
            }
        }

        return sb.ToString().Trim();
    }
}
