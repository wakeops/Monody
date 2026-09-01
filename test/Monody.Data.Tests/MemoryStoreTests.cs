using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using Monody.Data.Entities;
using Xunit;

namespace Monody.Data.Tests;

public class MemoryStoreTests : IDisposable
{
    private const ulong Alice = 111;
    private const ulong Bob = 222;

    private readonly SqliteFixture _fixture = new();
    private readonly MemoryStore _store;

    public MemoryStoreTests()
    {
        _store = new MemoryStore(_fixture.CreateFactory(), new FakeTimeProvider(DateTimeOffset.UnixEpoch));
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task RemembersAFact()
    {
        var result = await _store.RememberAsync(Alice, MemoryCategory.Location, "Lives in Raleigh, NC");

        Assert.True(result.Success);
        Assert.False(result.Replaced);

        var stored = await _store.GetAsync(Alice);
        Assert.Equal("Lives in Raleigh, NC", stored.Single().Content);
    }

    [Fact]
    public async Task ReplacesSingleValuedCategories()
    {
        // Moving house should update the fact, not accumulate a second one.
        await _store.RememberAsync(Alice, MemoryCategory.Location, "Lives in Raleigh, NC");
        var result = await _store.RememberAsync(Alice, MemoryCategory.Location, "Lives in Durham, NC");

        Assert.True(result.Success);
        Assert.True(result.Replaced);
        Assert.Equal("Lives in Durham, NC", (await _store.GetAsync(Alice)).Single().Content);
    }

    [Fact]
    public async Task KeepsSeveralPreferences()
    {
        await _store.RememberAsync(Alice, MemoryCategory.Preference, "Prefers metric units");
        await _store.RememberAsync(Alice, MemoryCategory.Preference, "Prefers concise answers");

        Assert.Equal(2, (await _store.GetAsync(Alice)).Count);
    }

    [Fact]
    public async Task IgnoresADuplicate()
    {
        await _store.RememberAsync(Alice, MemoryCategory.Preference, "Prefers metric units");
        var result = await _store.RememberAsync(Alice, MemoryCategory.Preference, "prefers METRIC units");

        Assert.True(result.Success);
        Assert.True(result.Duplicate);
        Assert.Single(await _store.GetAsync(Alice));
    }

    [Fact]
    public async Task RejectsContentThatIsTooLong()
    {
        var result = await _store.RememberAsync(Alice, MemoryCategory.Preference, new string('x', DataConstants.MaxMemoryLength + 1));

        Assert.False(result.Success);
        Assert.Empty(await _store.GetAsync(Alice));
    }

    [Fact]
    public async Task CapsTheNumberOfMemories()
    {
        for (var i = 0; i < DataConstants.MaxMemoriesPerUser; i++)
        {
            Assert.True((await _store.RememberAsync(Alice, MemoryCategory.Preference, $"Preference {i}")).Success);
        }

        var overflow = await _store.RememberAsync(Alice, MemoryCategory.Preference, "One too many");

        Assert.False(overflow.Success);
        Assert.Contains("maximum", overflow.Reason);
        Assert.Equal(DataConstants.MaxMemoriesPerUser, (await _store.GetAsync(Alice)).Count);
    }

    [Fact]
    public async Task KeepsUsersApart()
    {
        await _store.RememberAsync(Alice, MemoryCategory.Location, "Lives in Raleigh, NC");
        await _store.RememberAsync(Bob, MemoryCategory.Location, "Lives in Berlin");

        Assert.Equal("Lives in Raleigh, NC", (await _store.GetAsync(Alice)).Single().Content);
        Assert.Equal("Lives in Berlin", (await _store.GetAsync(Bob)).Single().Content);
    }

    [Fact]
    public async Task WillNotDeleteAnotherUsersMemory()
    {
        await _store.RememberAsync(Bob, MemoryCategory.Location, "Lives in Berlin");
        var bobsId = (await _store.GetAsync(Bob)).Single().Id;

        // Alice asking to delete Bob's row by id must do nothing at all.
        var deleted = await _store.ForgetAsync(Alice, [bobsId]);

        Assert.Equal(0, deleted);
        Assert.Single(await _store.GetAsync(Bob));
    }

    [Fact]
    public async Task ForgetsOnlyWhatWasAsked()
    {
        await _store.RememberAsync(Alice, MemoryCategory.Name, "Goes by Alice");
        await _store.RememberAsync(Alice, MemoryCategory.Preference, "Prefers metric units");

        var nameId = (await _store.GetAsync(Alice)).Single(m => m.Category == MemoryCategory.Name).Id;

        Assert.Equal(1, await _store.ForgetAsync(Alice, [nameId]));
        Assert.Equal(MemoryCategory.Preference, (await _store.GetAsync(Alice)).Single().Category);
    }

    [Fact]
    public async Task SupersedesAContradictoryPreference()
    {
        // Preferences are append-only, so changing one means forgetting the old one first.
        // This is the flow the prompt tells the model to follow.
        await _store.RememberAsync(Alice, MemoryCategory.Preference, "Prefers metric units");

        var stale = (await _store.GetAsync(Alice)).Single().Id;

        Assert.Equal(1, await _store.ForgetAsync(Alice, [stale]));
        await _store.RememberAsync(Alice, MemoryCategory.Preference, "Prefers imperial units");

        Assert.Equal("Prefers imperial units", (await _store.GetAsync(Alice)).Single().Content);
    }

    [Fact]
    public async Task ForgettingAnUnknownIdIsHarmless()
    {
        await _store.RememberAsync(Alice, MemoryCategory.Preference, "Prefers metric units");

        Assert.Equal(0, await _store.ForgetAsync(Alice, [4242]));
        Assert.Single(await _store.GetAsync(Alice));
    }

    [Fact]
    public async Task ForgetAllClearsOnlyThatUser()
    {
        await _store.RememberAsync(Alice, MemoryCategory.Name, "Goes by Alice");
        await _store.RememberAsync(Bob, MemoryCategory.Name, "Goes by Bob");

        Assert.Equal(1, await _store.ForgetAllAsync(Alice));
        Assert.Empty(await _store.GetAsync(Alice));
        Assert.Single(await _store.GetAsync(Bob));
    }
}
