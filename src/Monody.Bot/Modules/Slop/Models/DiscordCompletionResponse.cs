using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Monody.Bot.Modules.Slop.Models;

public sealed class DiscordCompletionResponse : IValidatableObject
{
    [Description("Which shape this response is using.")]
    [Required]
    public DiscordResponseKind Kind { get; set; }

    [Description("Plain text response. Must be non-empty when kind=Text. Use empty string when kind=Embed.")]
    [Required]
    [MaxLength(2000)]
    public string Text { get; set; }

    [Description("Discord embed JSON (single embed). Must be a valid embed when kind=Embed. Use an empty embed when kind=Text.")]
    [Required]
    public DiscordEmbed Embed { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Kind == DiscordResponseKind.Text)
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                yield return new ValidationResult(
                    "text is required when kind=text.",
                    [nameof(Text)]);
            }

            if (Embed is not null)
            {
                yield return new ValidationResult(
                    "embed must be null when kind=text.",
                    [nameof(Embed)]);
            }
        }
        else // kind == "embed"
        {
            if (Embed is null)
            {
                yield return new ValidationResult(
                    "embed is required when kind=embed.",
                    [nameof(Embed)]);
            }

            if (!string.IsNullOrWhiteSpace(Text))
            {
                yield return new ValidationResult(
                    "text must be null/empty when kind=embed.",
                    [nameof(Text)]);
            }
        }
    }
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

    [Description("URL for embed title link.")]
    [MaxLength(2048)]
    public string Url { get; set; }

    [Description("ISO 8601 timestamp (e.g., 2025-12-18T18:30:00Z).")]
    public string Timestamp { get; set; }

    [Description("Decimal RGB color (0 to 16777215).")]
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
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = default!;

    [Required]
    [MaxLength(1024)]
    public string Value { get; set; } = default!;

    public bool Inline { get; set; }
}