using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Monody.AI.Tools.Abstractions;
using Monody.AI.Tools.Capabilities.Memory;
using Monody.Data;
using Monody.Data.Entities;
using Xunit;

namespace Monody.AI.Tools.Tests;

/// <summary>
/// Covers the model-facing surface: what recall hands back, and that the tools act only for
/// whoever is in scope.
/// </summary>
public class MemoryPluginTests : IDisposable
{
    private const ulong Alice = 111;
    private const ulong Bob = 222;

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
    public async Task RecallReturnsIdsSoForgetCanNameOne()
    {
        using var _ = _invocationContext.BeginScope(Alice, null);

        await _plugin.RememberAsync(new RememberToolRequest
        {
            Category = MemoryCategory.Preference,
            Content = "Prefers metric units"
        });

        var recalled = (await _plugin.RecallAsync(new RecallToolRequest())).Memories.Single();

        Assert.NotEqual(0, recalled.Id);
        Assert.Equal("Preference", recalled.Category);
    }

    [Fact]
    public async Task ForgetsASupersededPreference()
    {
        using var _ = _invocationContext.BeginScope(Alice, null);

        await _plugin.RememberAsync(new RememberToolRequest { Category = MemoryCategory.Preference, Content = "Prefers metric units" });
        var stale = (await _plugin.RecallAsync(new RecallToolRequest())).Memories.Single().Id;

        var result = await _plugin.ForgetAsync(new ForgetToolRequest { MemoryId = stale });

        Assert.True(result.Forgotten);

        await _plugin.RememberAsync(new RememberToolRequest { Category = MemoryCategory.Preference, Content = "Prefers imperial units" });

        Assert.Equal("Prefers imperial units", (await _plugin.RecallAsync(new RecallToolRequest())).Memories.Single().Content);
    }

    [Fact]
    public async Task ForgettingAnIdThatIsNotYoursDoesNothing()
    {
        // The id is real, just somebody else's - the case a prompt injection would aim for.
        int bobsId;
        using (var _ = _invocationContext.BeginScope(Bob, null))
        {
            await _plugin.RememberAsync(new RememberToolRequest { Category = MemoryCategory.Preference, Content = "Bob's preference" });
            bobsId = (await _plugin.RecallAsync(new RecallToolRequest())).Memories.Single().Id;
        }

        using (var _ = _invocationContext.BeginScope(Alice, null))
        {
            var result = await _plugin.ForgetAsync(new ForgetToolRequest { MemoryId = bobsId });

            Assert.False(result.Forgotten);
        }

        using (var _ = _invocationContext.BeginScope(Bob, null))
        {
            Assert.Single((await _plugin.RecallAsync(new RecallToolRequest())).Memories);
        }
    }

    [Fact]
    public async Task RefusesToActWithNobodyInScope()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _plugin.RecallAsync(new RecallToolRequest()));
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
