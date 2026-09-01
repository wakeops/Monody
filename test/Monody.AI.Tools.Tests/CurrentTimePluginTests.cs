using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using Monody.AI.Tools.Capabilities.CurrentTime;
using Xunit;

namespace Monody.AI.Tools.Tests;

public class CurrentTimePluginTests
{
    // Mid-summer, so the northern-hemisphere zones below are on daylight saving time.
    private static readonly DateTimeOffset _now = new(2026, 7, 15, 12, 00, 00, TimeSpan.Zero);

    private static Task<CurrentTimeToolResponse> RunAsync(string timeZone) =>
        new CurrentTimePlugin(new FakeTimeProvider(_now))
            .GetCurrentTimeAsync(new CurrentTimeToolRequest { TimeZone = timeZone });

    [Fact]
    public async Task DefaultsToUtc()
    {
        var result = await RunAsync(null);

        Assert.Equal("UTC", result.TimeZone);
        Assert.Equal("2026-07-15T12:00:00Z", result.UtcTime);
        Assert.Equal("+00:00", result.UtcOffset);
        Assert.False(result.IsDaylightSavingTime);
    }

    [Fact]
    public async Task ResolvesAnIanaId()
    {
        var result = await RunAsync("Europe/London");

        Assert.Equal("Europe/London", result.TimeZone);
        Assert.Equal("+01:00", result.UtcOffset);
        Assert.True(result.IsDaylightSavingTime);
        Assert.Equal("BST", result.Abbreviation);
        Assert.Equal("Wednesday, 15 July 2026 13:00 BST", result.LocalTimeDescription);
    }

    [Theory]
    // Identifiers only, in the shapes .NET accepts, plus surrounding whitespace.
    [InlineData("Asia/Tokyo", "Asia/Tokyo")]
    [InlineData("  Europe/London  ", "Europe/London")]
    [InlineData("UTC", "UTC")]
    public async Task AcceptsATimeZoneIdentifier(string input, string expected)
    {
        Assert.Equal(expected, (await RunAsync(input)).TimeZone);
    }

    [Fact]
    public async Task AppliesTheZonesOffsetToLocalTime()
    {
        var tokyo = await RunAsync("Asia/Tokyo");

        // UTC+9, no daylight saving.
        Assert.Equal("2026-07-15T21:00:00+09:00", tokyo.LocalTime);
        Assert.Equal("+09:00", tokyo.UtcOffset);
        Assert.False(tokyo.IsDaylightSavingTime);
    }

    [Fact]
    public async Task FormatsNegativeOffsets()
    {
        var result = await RunAsync("America/New_York");

        Assert.Equal("-04:00", result.UtcOffset);
        Assert.Equal("2026-07-15T08:00:00-04:00", result.LocalTime);
    }

    [Fact]
    public async Task AppliesAZoneThatDoesNotObserveDaylightSaving()
    {
        // Proves the zone is really applied rather than just echoed: Arizona stays on MST.
        var result = await RunAsync("America/Phoenix");

        Assert.Equal("-07:00", result.UtcOffset);
        Assert.False(result.IsDaylightSavingTime);
    }

    [Theory]
    // The tool takes zones, not places. The model knows which zone a city is in, so it is told
    // to convert rather than have this guess - but the refusal has to say so, or it just stalls.
    [InlineData("Raleigh, NC")]
    [InlineData("Springfield")]
    [InlineData("the Isle of Skye")]
    [InlineData("Middle Earth")]
    public async Task RejectsAPlaceNameAndSaysWhatToSendInstead(string place)
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => RunAsync(place));

        Assert.Contains(place, ex.Message);
        Assert.Contains("America/New_York", ex.Message);
    }

    [Fact]
    public async Task EmitsDiscordTimestampMarkup()
    {
        // Renders in each reader's own local time, so it stays correct for everyone in the channel.
        Assert.Equal($"<t:{_now.ToUnixTimeSeconds()}:F>", (await RunAsync("Europe/London")).DiscordTimestamp);
    }

    [Fact]
    public async Task ReportsTheSameInstantAcrossZones()
    {
        var london = await RunAsync("Europe/London");
        var tokyo = await RunAsync("Asia/Tokyo");

        Assert.Equal(london.UtcTime, tokyo.UtcTime);
        Assert.Equal(
            DateTimeOffset.Parse(london.LocalTime).ToUniversalTime(),
            DateTimeOffset.Parse(tokyo.LocalTime).ToUniversalTime());
    }
}
