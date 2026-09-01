using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Monody.Bot.Modules.Slop.Models;

public sealed class DiscordCompletionResponse
{
    [Description("Text for prose, Embed for a titled card with structured fields.")]
    [Required]
    public DiscordResponseKind Kind { get; set; }

    [Description("Markdown response. Required when kind=Text. When kind=Embed this is an optional one-line lead-in shown above the card; use an empty string for none.")]
    [Required]
    [MaxLength(2000)]
    public string Text { get; set; }

    [Description("The card to render when kind=Embed. Fill title and description at minimum. Use an object with empty values when kind=Text.")]
    [Required]
    public DiscordEmbed Embed { get; set; }
}

public enum DiscordResponseKind
{
    Text,
    Embed
}

public sealed class DiscordEmbed
{
    [MaxLength(256)]
    public string Title { get; set; }

    [MaxLength(4096)]
    public string Description { get; set; }

    [Description("Makes the title a link. http(s) only; empty string for none.")]
    [MaxLength(2048)]
    public string Url { get; set; }

    [Description("ISO 8601 timestamp shown in the footer (e.g. 2025-12-18T18:30:00Z). Empty string for none.")]
    public string Timestamp { get; set; }

    [Description("Decimal RGB accent colour on the left edge (0 to 16777215). Use 0 to keep the default.")]
    [Range(0, 16_777_215)]
    public int Color { get; set; }

    public DiscordEmbedFooter Footer { get; set; }

    public DiscordEmbedImage Image { get; set; }

    public DiscordEmbedThumbnail Thumbnail { get; set; }

    public DiscordEmbedAuthor Author { get; set; }

    [MaxLength(25)]
    public List<DiscordEmbedField> Fields { get; set; }
}

public sealed class DiscordEmbedFooter
{
    [MaxLength(2048)]
    public string Text { get; set; }

    [MaxLength(2048)]
    public string IconUrl { get; set; }
}

public sealed class DiscordEmbedImage
{
    [MaxLength(2048)]
    public string Url { get; set; }
}

public sealed class DiscordEmbedThumbnail
{
    [MaxLength(2048)]
    public string Url { get; set; }
}

public sealed class DiscordEmbedAuthor
{
    [MaxLength(256)]
    public string Name { get; set; }

    [MaxLength(2048)]
    public string Url { get; set; }

    [MaxLength(2048)]
    public string IconUrl { get; set; }
}

public sealed class DiscordEmbedField
{
    [Description("Short label for this row, e.g. a stat name or a heading.")]
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = default!;

    [Description("The value for this row. Supports Markdown.")]
    [Required]
    [MaxLength(1024)]
    public string Value { get; set; } = default!;

    [Description("True to sit this field beside others in a column layout; use for short values.")]
    public bool Inline { get; set; }
}