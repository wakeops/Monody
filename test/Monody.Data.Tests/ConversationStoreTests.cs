using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using Monody.Data.Entities;
using Xunit;

namespace Monody.Data.Tests;

public class ConversationStoreTests : IDisposable
{
    private const ulong Interaction = 1234567890123456789;
    private const ulong Alice = 111;
    private const ulong Channel = 999;

    private static readonly DateTimeOffset _now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteFixture _fixture = new();
    private readonly FakeTimeProvider _time = new(_now);
    private readonly ConversationStore _store;

    public ConversationStoreTests()
    {
        _store = new ConversationStore(_fixture.CreateFactory(), _time);
    }

    public void Dispose() => _fixture.Dispose();

    private static List<ConversationTurn> Turns(params string[] contents) =>
        [.. contents.Select((c, i) => new ConversationTurn(i % 2 == 0 ? "user" : "assistant", c))];

    [Fact]
    public async Task RoundTripsAConversation()
    {
        await _store.SaveAsync(Interaction, Alice, Channel, null, Turns("what is the time?", "half past two"));

        var turns = await _store.GetTurnsAsync(Interaction);

        Assert.Equal(2, turns.Count);
        Assert.Equal("user", turns[0].Role);
        Assert.Equal("half past two", turns[1].Content);
    }

    [Fact]
    public async Task SurvivesTheProcessThatWroteIt()
    {
        // The whole point: a redeploy used to drop every conversation, which is what produced
        // "Sorry, I lost this conversation's context" on the next follow-up.
        await _store.SaveAsync(Interaction, Alice, Channel, null, Turns("remember this"));

        var afterRestart = new ConversationStore(_fixture.CreateFactory(), new FakeTimeProvider(_now));

        Assert.True(await afterRestart.ExistsAsync(Interaction));
        Assert.Equal("remember this", (await afterRestart.GetTurnsAsync(Interaction)).Single().Content);
    }

    [Fact]
    public async Task ReportsAnUnknownConversationAsMissing()
    {
        Assert.False(await _store.ExistsAsync(Interaction));
        Assert.Null(await _store.GetTurnsAsync(Interaction));
    }

    [Fact]
    public async Task ReplacesTheTurnsOnFollowUp()
    {
        await _store.SaveAsync(Interaction, Alice, Channel, null, Turns("first"));
        await _store.SaveAsync(Interaction, Alice, Channel, null, Turns("first", "reply", "second"));

        Assert.Equal(3, (await _store.GetTurnsAsync(Interaction)).Count);
    }

    [Fact]
    public async Task KeepsOnlyTheMostRecentTurns()
    {
        var many = Turns([.. Enumerable.Range(0, ConversationStore.MaxTurns + 10).Select(i => $"turn {i}")]);

        await _store.SaveAsync(Interaction, Alice, Channel, null, many);

        var stored = await _store.GetTurnsAsync(Interaction);

        Assert.Equal(ConversationStore.MaxTurns, stored.Count);
        Assert.Equal(many[^1].Content, stored[^1].Content);
    }

    [Fact]
    public async Task PrunesOnlyConversationsPastRetention()
    {
        await _store.SaveAsync(Interaction, Alice, Channel, null, Turns("old"));

        _time.Advance(ConversationStore.RetentionPeriod + TimeSpan.FromDays(1));
        await _store.SaveAsync(2, Alice, Channel, null, Turns("fresh"));

        Assert.Equal(1, await _store.PruneAsync());
        Assert.False(await _store.ExistsAsync(Interaction));
        Assert.True(await _store.ExistsAsync(2));
    }

    [Fact]
    public async Task StartsOverRatherThanFailingOnUnreadableTurns()
    {
        // A row written by an older shape should cost the thread, not the command.
        await using (var db = new MonodyDbContext(_fixture.Options))
        {
            db.Conversations.Add(new Conversation
            {
                Id = Interaction,
                UserId = Alice,
                TurnsJson = "{not json}",
                CreatedAt = _now,
                UpdatedAt = _now
            });
            await db.SaveChangesAsync();
        }

        Assert.Empty(await _store.GetTurnsAsync(Interaction));
    }
}
