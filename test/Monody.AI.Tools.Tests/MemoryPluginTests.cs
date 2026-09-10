using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Monody.AI.Tools.Abstractions;
using Monody.AI.Tools.Capabilities.Memory;
using Monody.Data;
using Monody.Data.Stores;
using Xunit;

namespace Monody.AI.Tools.Tests;

/// <summary>
/// Covers the model-facing surface: what recall_index/recall_topic hand back, and that the tools
/// act only for whoever is in scope.
/// </summary>
public class MemoryPluginTests : IDisposable
{
    private const ulong _alice = 111;
    private const ulong _bob = 222;

    private readonly SqliteConnection _connection;
    private readonly MemoryStore _store;
    private readonly StubInvocationContext _invocationContext = new();
    private readonly MemoryPlugin _plugin;

    public MemoryPluginTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<MonodyDbContext>().UseSqlite(_connection).Options;
        using (var db = new MonodyDbContext(options))
        {
            db.Database.EnsureCreated();
        }

        _store = new MemoryStore(new Factory(options), TimeProvider.System);
        _plugin = new MemoryPlugin(_store, _invocationContext);
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task RecallIndexReturnsIdsAndSlugsSoForgetAndRecallTopicCanUseThem()
    {
        using var _ = _invocationContext.BeginScope(_alice, null);

        await _plugin.RememberAsync(new RememberToolRequest
        {
            Slug = "units",
            Description = "Preferred units",
            Content = "Prefers metric units"
        });

        var entry = (await _plugin.RecallIndexAsync()).Topics.Single();

        Assert.NotEqual(0, entry.Id);
        Assert.Equal("units", entry.Slug);
        Assert.Equal("Preferred units", entry.Description);
    }

    [Fact]
    public async Task RecallTopicReturnsFullContent()
    {
        using var _ = _invocationContext.BeginScope(_alice, null);

        await _plugin.RememberAsync(new RememberToolRequest
        {
            Slug = "units",
            Description = "Preferred units",
            Content = "Prefers metric units"
        });

        var found = await _plugin.RecallTopicAsync(new RecallTopicToolRequest { Slug = "units" });
        Assert.True(found.Found);
        Assert.Equal("Prefers metric units", found.Content);

        var missing = await _plugin.RecallTopicAsync(new RecallTopicToolRequest { Slug = "unknown" });
        Assert.False(missing.Found);
    }

    [Fact]
    public async Task RememberingTheSameSlugAgainUpdatesRatherThanDuplicating()
    {
        using var _ = _invocationContext.BeginScope(_alice, null);

        await _plugin.RememberAsync(new RememberToolRequest { Slug = "units", Description = "Preferred units", Content = "Prefers metric units" });
        await _plugin.RememberAsync(new RememberToolRequest { Slug = "units", Description = "Preferred units", Content = "Prefers imperial units" });

        Assert.Single((await _plugin.RecallIndexAsync()).Topics);

        var topic = await _plugin.RecallTopicAsync(new RecallTopicToolRequest { Slug = "units" });
        Assert.Equal("Prefers imperial units", topic.Content);
    }

    [Fact]
    public async Task ForgettingAnIdThatIsNotYoursDoesNothing()
    {
        // The id is real, just somebody else's - the case a prompt injection would aim for.
        int bobsId;
        using (var _ = _invocationContext.BeginScope(_bob, null))
        {
            await _plugin.RememberAsync(new RememberToolRequest { Slug = "units", Description = "Preferred units", Content = "Bob's preference" });
            bobsId = (await _plugin.RecallIndexAsync()).Topics.Single().Id;
        }

        using (var _ = _invocationContext.BeginScope(_alice, null))
        {
            var result = await _plugin.ForgetAsync(new ForgetToolRequest { MemoryId = bobsId });

            Assert.False(result.Forgotten);
        }

        using (var _ = _invocationContext.BeginScope(_bob, null))
        {
            Assert.Single((await _plugin.RecallIndexAsync()).Topics);
        }
    }

    [Fact]
    public async Task RefusesToActWithNobodyInScope()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _plugin.RecallIndexAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => _plugin.RecallTopicAsync(new RecallTopicToolRequest { Slug = "units" }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _plugin.ForgetAsync(new ForgetToolRequest { MemoryId = 1 }));
    }

    private sealed class StubInvocationContext : IInvocationContext
    {
        public ulong? UserId { get; private set; }

        public ulong? ChannelId { get; private set; }

        public IDisposable BeginScope(ulong userId, ulong? channelId)
        {
            UserId = userId;
            ChannelId = channelId;
            return new Reset(this);
        }

        private sealed class Reset : IDisposable
        {
            private readonly StubInvocationContext _context;

            public Reset(StubInvocationContext context) => _context = context;

            public void Dispose()
            {
                _context.UserId = null;
                _context.ChannelId = null;
            }
        }
    }

    private sealed class Factory : IDbContextFactory<MonodyDbContext>
    {
        private readonly DbContextOptions<MonodyDbContext> _options;

        public Factory(DbContextOptions<MonodyDbContext> options) => _options = options;

        public MonodyDbContext CreateDbContext() => new(_options);
    }
}
