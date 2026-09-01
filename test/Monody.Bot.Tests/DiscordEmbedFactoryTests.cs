using System.Collections.Generic;
using System.Linq;
using Discord;
using Monody.Bot.Modules;
using Monody.Bot.Modules.Slop.Models;
using Monody.Bot.Modules.Slop.Utils;
using Xunit;

namespace Monody.Bot.Tests;

public class DiscordEmbedFactoryTests
{
    /// <summary>The shape strict structured outputs actually produce: every property present.</summary>
    private static DiscordEmbed Model(
        string title = "Title",
        string description = "Description",
        string url = "",
        string timestamp = "",
        int color = 0,
        List<DiscordEmbedField> fields = null) => new()
        {
            Title = title,
            Description = description,
            Url = url,
            Timestamp = timestamp,
            Color = color,
            Footer = new DiscordEmbedFooter { Text = "", IconUrl = "" },
            Image = new DiscordEmbedImage { Url = "" },
            Thumbnail = new DiscordEmbedThumbnail { Url = "" },
            Author = new DiscordEmbedAuthor { Name = "", Url = "", IconUrl = "" },
            Fields = fields ?? []
        };

    [Fact]
    public void MapsEveryPartOfTheModel()
    {
        var model = Model(url: "https://example.com/article", timestamp: "2025-12-18T18:30:00Z", color: 0x00FF00);
        model.Author = new DiscordEmbedAuthor { Name = "Monody", Url = "https://example.com", IconUrl = "https://example.com/a.png" };
        model.Footer = new DiscordEmbedFooter { Text = "Source", IconUrl = "https://example.com/f.png" };
        model.Image = new DiscordEmbedImage { Url = "https://example.com/i.png" };
        model.Thumbnail = new DiscordEmbedThumbnail { Url = "https://example.com/t.png" };
        model.Fields = [new DiscordEmbedField { Name = "Stat", Value = "42", Inline = true }];

        var embed = DiscordEmbedFactory.TryBuild(model);

        Assert.Equal("Title", embed.Title);
        Assert.Equal("Description", embed.Description);
        Assert.Equal("https://example.com/article", embed.Url);
        Assert.Equal(new Color(0x00FF00), embed.Color);
        Assert.NotNull(embed.Timestamp);
        Assert.Equal("Monody", embed.Author.Value.Name);
        Assert.Equal("Source", embed.Footer.Value.Text);
        Assert.Equal("https://example.com/i.png", embed.Image.Value.Url);
        Assert.Equal("https://example.com/t.png", embed.Thumbnail.Value.Url);
        Assert.Equal("Stat", embed.Fields.Single().Name);
        Assert.True(embed.Fields.Single().Inline);
    }

    [Fact]
    public void TreatsEmptyStringsAsAbsent()
    {
        // Strict mode forces the model to send empty placeholders rather than omitting them;
        // setting an empty footer or author would render a blank line on the card.
        var embed = DiscordEmbedFactory.TryBuild(Model());

        Assert.Null(embed.Footer);
        Assert.Null(embed.Author);
        Assert.Null(embed.Image);
        Assert.Null(embed.Thumbnail);
        Assert.Null(embed.Timestamp);
        Assert.Null(embed.Url);
    }

    [Fact]
    public void KeepsAnImageOnlyEmbed()
    {
        // EmbedBuilder.Length counts text but not image urls, so an image-only card measures
        // zero and must not be mistaken for an empty one.
        var model = Model(title: "", description: "");
        model.Image = new DiscordEmbedImage { Url = "https://example.com/i.png" };

        var embed = DiscordEmbedFactory.TryBuild(model);

        Assert.NotNull(embed);
        Assert.Equal("https://example.com/i.png", embed.Image.Value.Url);
    }

    [Fact]
    public void FallsBackToBrandColourWhenUnset()
    {
        Assert.Equal(new Color(MonodyConstants.DefaultEmbedColor), DiscordEmbedFactory.TryBuild(Model(color: 0)).Color);
    }

    [Fact]
    public void ReturnsNullWhenNothingIsRenderable()
    {
        // The caller needs to fall back to plain text rather than send an empty card.
        Assert.Null(DiscordEmbedFactory.TryBuild(Model(title: "", description: "")));
        Assert.Null(DiscordEmbedFactory.TryBuild(Model(title: "   ", description: "\n")));
        Assert.Null(DiscordEmbedFactory.TryBuild(null));
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com/x")]
    [InlineData("/relative/path")]
    public void DropsUrlsDiscordWouldReject(string url)
    {
        // Discord.Net throws on a non-http(s) url at Build(), which would fail the whole send.
        var model = Model(url: url);
        model.Image = new DiscordEmbedImage { Url = url };

        var embed = DiscordEmbedFactory.TryBuild(model);

        Assert.Null(embed.Url);
        Assert.Null(embed.Image);
    }

    [Fact]
    public void ClampsOversizeValuesToDiscordLimits()
    {
        var model = Model(title: new string('t', 500), description: new string('d', 5000));
        model.Fields = [new DiscordEmbedField { Name = new string('n', 400), Value = new string('v', 2000) }];

        var embed = DiscordEmbedFactory.TryBuild(model);

        Assert.Equal(EmbedBuilder.MaxTitleLength, embed.Title.Length);
        Assert.Equal(EmbedBuilder.MaxDescriptionLength, embed.Description.Length);
        Assert.Equal(EmbedFieldBuilder.MaxFieldNameLength, embed.Fields.Single().Name.Length);
        Assert.Equal(EmbedFieldBuilder.MaxFieldValueLength, embed.Fields.Single().Value.Length);
    }

    [Fact]
    public void DropsFieldsBeyondDiscordsCount()
    {
        var fields = Enumerable.Range(0, 40)
            .Select(i => new DiscordEmbedField { Name = $"f{i}", Value = "v" })
            .ToList();

        var embed = DiscordEmbedFactory.TryBuild(Model(fields: fields));

        Assert.Equal(EmbedBuilder.MaxFieldCount, embed.Fields.Length);
    }

    [Fact]
    public void SkipsFieldsMissingEitherHalf()
    {
        // Discord.Net throws on an empty field name or value.
        var fields = new List<DiscordEmbedField>
        {
            new() { Name = "", Value = "orphaned value" },
            new() { Name = "orphaned name", Value = "  " },
            new() { Name = "kept", Value = "value" }
        };

        var embed = DiscordEmbedFactory.TryBuild(Model(fields: fields));

        Assert.Equal("kept", embed.Fields.Single().Name);
    }

    [Fact]
    public void StaysWithinTheTotalEmbedBudget()
    {
        // Every individual value is legal, but together they blow the 6000 total that
        // Discord.Net only checks at Build().
        var fields = Enumerable.Range(0, 25)
            .Select(i => new DiscordEmbedField { Name = $"field{i}", Value = new string('v', 1000) })
            .ToList();

        var embed = DiscordEmbedFactory.TryBuild(Model(description: new string('d', 4000), fields: fields));

        Assert.True(
            embed.Length <= EmbedBuilder.MaxEmbedLength,
            $"embed length {embed.Length} exceeds Discord's {EmbedBuilder.MaxEmbedLength} limit");
    }

    [Fact]
    public void KeepsTitleAndDescriptionWhenFieldsExhaustTheBudget()
    {
        // The lead content matters more than the trailing fields, so fields are what gets dropped.
        var fields = Enumerable.Range(0, 25)
            .Select(i => new DiscordEmbedField { Name = $"field{i}", Value = new string('v', 1000) })
            .ToList();

        var embed = DiscordEmbedFactory.TryBuild(Model(description: new string('d', 4000), fields: fields));

        Assert.Equal("Title", embed.Title);
        Assert.Equal(4000, embed.Description.Length);
        Assert.NotEmpty(embed.Fields);
    }
}
