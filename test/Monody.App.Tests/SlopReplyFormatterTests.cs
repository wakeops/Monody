using Discord;
using Monody.App.Modules.Slop.Utils;
using Xunit;

namespace Monody.App.Tests;

public class SlopReplyFormatterTests
{
    [Fact]
    public void Build_QuotesPromptAboveAnswer()
    {
        var reply = SlopReplyFormatter.Build("what's the weather?", "Sunny.");

        Assert.Equal("> what's the weather?\nSunny.", reply);
    }

    [Fact]
    public void Build_QuotesEveryLineOfAMultiLinePrompt()
    {
        var reply = SlopReplyFormatter.Build("first\r\n\r\nsecond", "Answer");

        Assert.Equal("> first\n>\n> second\nAnswer", reply);
    }

    [Fact]
    public void Build_WithoutBody_ReturnsJustTheQuote()
    {
        // An embed-only answer still gets the prompt as its lead-in.
        Assert.Equal("> prompt", SlopReplyFormatter.Build("prompt", ""));
    }

    [Fact]
    public void Build_WithoutPrompt_ReturnsJustTheBody()
    {
        Assert.Equal("Answer", SlopReplyFormatter.Build("  ", "Answer"));
        Assert.Null(SlopReplyFormatter.Build(null, null));
    }

    [Fact]
    public void Build_ClipsLongPrompts()
    {
        var reply = SlopReplyFormatter.Build(new string('p', 1800), "Answer");

        var quote = reply!.Split('\n')[0];
        Assert.Equal("> ".Length + SlopReplyFormatter.MaxQuotedPromptLength, quote.Length);
        Assert.EndsWith("…", quote);
    }

    [Fact]
    public void Build_KeepsTheWholeMessageWithinDiscordsLimit()
    {
        var reply = SlopReplyFormatter.Build(new string('p', 1800), new string('a', 5000));

        Assert.Equal(DiscordConfig.MaxMessageSize, reply!.Length);
        Assert.EndsWith("…", reply);
    }

    [Fact]
    public void Build_DoesNotSplitASurrogatePair()
    {
        var prompt = new string('p', SlopReplyFormatter.MaxQuotedPromptLength - 2) + "😀😀";

        var quote = SlopReplyFormatter.Build(prompt, null)!;

        Assert.False(char.IsHighSurrogate(quote[^2]));
    }
}
