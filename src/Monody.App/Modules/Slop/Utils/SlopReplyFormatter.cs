using Discord;

namespace Monody.App.Modules.Slop.Utils;

/// <summary>
/// Builds the text of a <c>/slop</c> reply: the prompt that triggered it, quoted, then the answer.
/// </summary>
internal static class SlopReplyFormatter
{
    /// <summary>Long prompts are clipped in the quote so the answer keeps most of the message.</summary>
    internal const int MaxQuotedPromptLength = 300;

    public static string? Build(string? prompt, string? body)
    {
        var quote = Quote(prompt);
        var hasBody = !string.IsNullOrWhiteSpace(body);

        if (quote is null)
        {
            return hasBody ? Truncate(body!, DiscordConfig.MaxMessageSize) : null;
        }

        if (!hasBody)
        {
            return quote;
        }

        var budget = DiscordConfig.MaxMessageSize - quote.Length - 1;

        return $"{quote}\n{Truncate(body!, budget)}";
    }

    private static string? Quote(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return null;
        }

        var clipped = Truncate(prompt.Trim(), MaxQuotedPromptLength);

        var lines = clipped
            .Split('\n')
            .Select(line => $"> {line.TrimEnd('\r')}".TrimEnd());

        return string.Join('\n', lines);
    }

    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        var cut = maxLength - 1;

        // Don't split a surrogate pair and leave half an emoji before the ellipsis.
        if (char.IsHighSurrogate(text[cut - 1]))
        {
            cut--;
        }

        return text[..cut] + "…";
    }
}
