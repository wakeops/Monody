using Microsoft.Extensions.Time.Testing;
using Monody.Data.Stores;
using Xunit;

namespace Monody.Data.Tests;

public class MemoryStoreTests : IDisposable
{
    private const ulong _alice = 111;
    private const ulong _bob = 222;

    private readonly SqliteFixture _fixture = new();
    private readonly FakeTimeProvider _timeProvider = new(DateTimeOffset.UnixEpoch);
    private readonly MemoryStore _store;

    public MemoryStoreTests()
    {
        _store = new MemoryStore(_fixture.CreateFactory(), _timeProvider);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task RemembersATopic()
    {
        var result = await _store.RememberAsync(_alice, "home-location", "Where the user lives", "Lives in Raleigh, NC");

        Assert.True(result.Success);
        Assert.False(result.Replaced);

        var stored = await _store.GetAsync(_alice);
        var topic = stored.Single();
        Assert.Equal("home-location", topic.Slug);
        Assert.Equal("Where the user lives", topic.Description);
        Assert.Equal("Lives in Raleigh, NC", topic.Content);
    }

    [Fact]
    public async Task RememberingTheSameSlugUpdatesInPlace()
    {
        // Moving house should update the topic, not accumulate a second one.
        await _store.RememberAsync(_alice, "home-location", "Where the user lives", "Lives in Raleigh, NC");
        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        var result = await _store.RememberAsync(_alice, "home-location", "Where the user lives", "Lives in Durham, NC");

        Assert.True(result.Success);
        Assert.True(result.Replaced);

        var topic = (await _store.GetAsync(_alice)).Single();
        Assert.Equal("Lives in Durham, NC", topic.Content);
        Assert.True(topic.UpdatedAt > topic.CreatedAt);
    }

    [Fact]
    public async Task RememberingDifferentSlugsBothPersist()
    {
        await _store.RememberAsync(_alice, "units", "Preferred units", "Prefers metric units");
        await _store.RememberAsync(_alice, "answer-style", "Preferred answer style", "Prefers concise answers");

        Assert.Equal(2, (await _store.GetAsync(_alice)).Count);
    }

    [Fact]
    public async Task IgnoresAnIdenticalRemember()
    {
        await _store.RememberAsync(_alice, "units", "Preferred units", "Prefers metric units");
        var result = await _store.RememberAsync(_alice, "units", "preferred UNITS", "prefers METRIC units");

        Assert.True(result.Success);
        Assert.True(result.Duplicate);
        Assert.Single(await _store.GetAsync(_alice));
    }

    [Fact]
    public async Task RejectsContentThatIsTooLong()
    {
        var result = await _store.RememberAsync(_alice, "units", "Preferred units", new string('x', DataConstants.MaxMemoryContentLength + 1));

        Assert.False(result.Success);
        Assert.Empty(await _store.GetAsync(_alice));
    }

    [Fact]
    public async Task RejectsADescriptionThatIsTooLong()
    {
        var result = await _store.RememberAsync(_alice, "units", new string('x', DataConstants.MaxMemoryDescriptionLength + 1), "Prefers metric units");

        Assert.False(result.Success);
        Assert.Empty(await _store.GetAsync(_alice));
    }

    [Theory]
    [InlineData("")]
    [InlineData("preferred units")]
    [InlineData("preferred_units")]
    [InlineData("-units")]
    [InlineData("units-")]
    public async Task RejectsAnInvalidSlug(string slug)
    {
        var result = await _store.RememberAsync(_alice, slug, "Preferred units", "Prefers metric units");

        Assert.False(result.Success);
        Assert.Empty(await _store.GetAsync(_alice));
    }

    [Fact]
    public async Task NormalizesAMixedCaseSlug()
    {
        var result = await _store.RememberAsync(_alice, "Units", "Preferred units", "Prefers metric units");

        Assert.True(result.Success);
        Assert.Equal("units", (await _store.GetAsync(_alice)).Single().Slug);
    }

    [Fact]
    public async Task RejectsASlugThatIsTooLong()
    {
        var result = await _store.RememberAsync(_alice, new string('a', DataConstants.MaxSlugLength + 1), "Preferred units", "Prefers metric units");

        Assert.False(result.Success);
        Assert.Empty(await _store.GetAsync(_alice));
    }

    [Fact]
    public async Task CapsTheNumberOfMemories()
    {
        for (var i = 0; i < DataConstants.MaxMemoriesPerUser; i++)
        {
            Assert.True((await _store.RememberAsync(_alice, $"topic-{i}", "A topic", $"Content {i}")).Success);
        }

        var overflow = await _store.RememberAsync(_alice, "one-too-many", "A topic", "One too many");

        Assert.False(overflow.Success);
        Assert.Contains("maximum", overflow.Reason);
        Assert.Equal(DataConstants.MaxMemoriesPerUser, (await _store.GetAsync(_alice)).Count);
    }

    [Fact]
    public async Task UpdatingAnExistingSlugDoesNotCountAgainstTheCap()
    {
        for (var i = 0; i < DataConstants.MaxMemoriesPerUser; i++)
        {
            await _store.RememberAsync(_alice, $"topic-{i}", "A topic", $"Content {i}");
        }

        var update = await _store.RememberAsync(_alice, "topic-0", "A topic", "Updated content");

        Assert.True(update.Success);
        Assert.True(update.Replaced);
        Assert.Equal(DataConstants.MaxMemoriesPerUser, (await _store.GetAsync(_alice)).Count);
    }

    [Fact]
    public async Task KeepsUsersApart()
    {
        await _store.RememberAsync(_alice, "home-location", "Where the user lives", "Lives in Raleigh, NC");
        await _store.RememberAsync(_bob, "home-location", "Where the user lives", "Lives in Berlin");

        Assert.Equal("Lives in Raleigh, NC", (await _store.GetAsync(_alice)).Single().Content);
        Assert.Equal("Lives in Berlin", (await _store.GetAsync(_bob)).Single().Content);
    }

    [Fact]
    public async Task WillNotDeleteAnotherUsersMemory()
    {
        await _store.RememberAsync(_bob, "home-location", "Where the user lives", "Lives in Berlin");
        var bobsId = (await _store.GetAsync(_bob)).Single().Id;

        // Alice asking to delete Bob's row by id must do nothing at all.
        var deleted = await _store.ForgetAsync(_alice, [bobsId]);

        Assert.Equal(0, deleted);
        Assert.Single(await _store.GetAsync(_bob));
    }

    [Fact]
    public async Task ForgetsOnlyWhatWasAsked()
    {
        await _store.RememberAsync(_alice, "name", "What the user is called", "Goes by Alice");
        await _store.RememberAsync(_alice, "units", "Preferred units", "Prefers metric units");

        var nameId = (await _store.GetAsync(_alice)).Single(m => m.Slug == "name").Id;

        Assert.Equal(1, await _store.ForgetAsync(_alice, [nameId]));
        Assert.Equal("units", (await _store.GetAsync(_alice)).Single().Slug);
    }

    [Fact]
    public async Task ForgettingAnUnknownIdIsHarmless()
    {
        await _store.RememberAsync(_alice, "units", "Preferred units", "Prefers metric units");

        Assert.Equal(0, await _store.ForgetAsync(_alice, [4242]));
        Assert.Single(await _store.GetAsync(_alice));
    }

    [Fact]
    public async Task ForgetAllClearsOnlyThatUser()
    {
        await _store.RememberAsync(_alice, "name", "What the user is called", "Goes by Alice");
        await _store.RememberAsync(_bob, "name", "What the user is called", "Goes by Bob");

        Assert.Equal(1, await _store.ForgetAllAsync(_alice));
        Assert.Empty(await _store.GetAsync(_alice));
        Assert.Single(await _store.GetAsync(_bob));
    }

    [Fact]
    public async Task IndexReturnsAllTopics()
    {
        await _store.RememberAsync(_alice, "name", "What the user is called", "Goes by Alice");
        await _store.RememberAsync(_alice, "units", "Preferred units", "Prefers metric units");

        var index = await _store.GetIndexAsync(_alice);

        Assert.Equal(["name", "units"], index.Select(m => m.Slug).OrderBy(s => s));
    }

    [Fact]
    public async Task GetTopicReturnsFullContentBySlug()
    {
        await _store.RememberAsync(_alice, "home-location", "Where the user lives", "Lives in Raleigh, NC");

        var topic = await _store.GetTopicAsync(_alice, "home-location");
        Assert.NotNull(topic);
        Assert.Equal("Lives in Raleigh, NC", topic.Content);

        Assert.Null(await _store.GetTopicAsync(_alice, "unknown-slug"));
    }

    [Fact]
    public async Task GetTopicIsScopedToTheUser()
    {
        await _store.RememberAsync(_bob, "home-location", "Where the user lives", "Lives in Berlin");

        Assert.Null(await _store.GetTopicAsync(_alice, "home-location"));
    }
}
