using System;
using System.Globalization;
using Discord;
using Monody.Bot.Modules.Slop.Models;

namespace Monody.Bot.Modules.Slop.Utils;

/// <summary>
/// Turns the model's requested embed into one Discord will accept.
/// </summary>
/// <remarks>
/// Nothing here trusts the model. Discord.Net throws on an over-long title, an empty field
/// name, more than 25 fields, a non-HTTP url, or a total payload over 6000 characters, and the
/// schema's [MaxLength] hints are advisory - OpenAI does not guarantee enforcing them. So every
/// value is clamped, and anything still unusable is dropped rather than allowed to fail the
/// send. Strict structured outputs also force every property to be present, so the model signals
/// "unused" with an empty string or an empty object; those are treated as absent.
/// </remarks>
public static class DiscordEmbedFactory
{
    /// <summary>
    /// Builds an embed, or returns null when the model asked for one but supplied nothing
    /// renderable, so the caller can fall back to a plain message.
    /// </summary>
    public static Embed TryBuild(DiscordEmbed model)
    {
        if (model is null)
        {
            return null;
        }

        // Discord caps the sum of title, description, author name, footer text and every field
        // name and value. Spend that budget in priority order and drop whatever no longer fits.
        var budget = EmbedBuilder.MaxEmbedLength;

        var title = Take(model.Title, EmbedBuilder.MaxTitleLength, ref budget);
        var description = Take(model.Description, EmbedBuilder.MaxDescriptionLength, ref budget);

        var builder = new EmbedBuilder()
            .WithColor(ResolveColor(model.Color));

        if (title is not null)
        {
            builder.WithTitle(title);

            // Discord only renders a title link, and only when there is a title to hang it on.
            if (TryGetHttpUrl(model.Url) is { } titleUrl)
            {
                builder.WithUrl(titleUrl);
            }
        }

        if (description is not null)
        {
            builder.WithDescription(description);
        }

        if (BuildAuthor(model.Author, ref budget) is { } author)
        {
            builder.WithAuthor(author);
        }

        if (BuildFooter(model.Footer, ref budget) is { } footer)
        {
            builder.WithFooter(footer);
        }

        if (TryParseTimestamp(model.Timestamp) is { } timestamp)
        {
            builder.WithTimestamp(timestamp);
        }

        if (TryGetHttpUrl(model.Image?.Url) is { } imageUrl)
        {
            builder.WithImageUrl(imageUrl);
        }

        if (TryGetHttpUrl(model.Thumbnail?.Url) is { } thumbnailUrl)
        {
            builder.WithThumbnailUrl(thumbnailUrl);
        }

        AddFields(builder, model, ref budget);

        // An embed carrying only a colour renders as a bare coloured bar, and Discord rejects
        // one that is entirely empty.
        return builder.Length > 0 || builder.ImageUrl is not null || builder.ThumbnailUrl is not null
            ? builder.Build()
            : null;
    }

    private static void AddFields(EmbedBuilder builder, DiscordEmbed model, ref int budget)
    {
        if (model.Fields is null)
        {
            return;
        }

        foreach (var field in model.Fields)
        {
            if (builder.Fields.Count >= EmbedBuilder.MaxFieldCount)
            {
                return;
            }

            // Both halves must survive clamping: Discord rejects a field with either side empty.
            var name = Take(field?.Name, EmbedFieldBuilder.MaxFieldNameLength, ref budget);
            if (name is null)
            {
                continue;
            }

            var value = Take(field.Value, EmbedFieldBuilder.MaxFieldValueLength, ref budget);
            if (value is null)
            {
                // Give back the name's budget, since the field is being dropped.
                budget += name.Length;
                continue;
            }

            builder.AddField(name, value, field.Inline);
        }
    }

    private static EmbedAuthorBuilder BuildAuthor(DiscordEmbedAuthor author, ref int budget)
    {
        var name = Take(author?.Name, EmbedAuthorBuilder.MaxAuthorNameLength, ref budget);
        if (name is null)
        {
            return null;
        }

        var builder = new EmbedAuthorBuilder().WithName(name);

        if (TryGetHttpUrl(author.Url) is { } url)
        {
            builder.WithUrl(url);
        }

        if (TryGetHttpUrl(author.IconUrl) is { } iconUrl)
        {
            builder.WithIconUrl(iconUrl);
        }

        return builder;
    }

    private static EmbedFooterBuilder BuildFooter(DiscordEmbedFooter footer, ref int budget)
    {
        var text = Take(footer?.Text, EmbedFooterBuilder.MaxFooterTextLength, ref budget);
        if (text is null)
        {
            return null;
        }

        var builder = new EmbedFooterBuilder().WithText(text);

        if (TryGetHttpUrl(footer.IconUrl) is { } iconUrl)
        {
            builder.WithIconUrl(iconUrl);
        }

        return builder;
    }

    /// <summary>
    /// Trims <paramref name="value"/> to whatever fits in both its own cap and the remaining
    /// total budget, charging what it takes. Returns null when it is blank or nothing is left.
    /// </summary>
    private static string Take(string value, int maxLength, ref int budget)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var allowed = Math.Min(maxLength, budget);

        if (allowed <= 0)
        {
            return null;
        }

        if (trimmed.Length > allowed)
        {
            trimmed = trimmed[..allowed];
        }

        budget -= trimmed.Length;
        return trimmed;
    }

    private static Color ResolveColor(int color) =>
        // The schema forces a value, so the model emits 0 when it has no opinion. Black is
        // indistinguishable from unset, so treat it as "use the brand colour".
        color is > 0 and <= 0xFFFFFF ? new Color((uint)color) : new Color(MonodyConstants.DefaultEmbedColor);

    /// <summary>Discord only accepts http(s) urls here, and throws on anything else.</summary>
    private static string TryGetHttpUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.AbsoluteUri
            : null;
    }

    private static DateTimeOffset? TryParseTimestamp(string timestamp)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
        {
            return null;
        }

        return DateTimeOffset.TryParse(
            timestamp.Trim(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsed)
            ? parsed
            : null;
    }
}
