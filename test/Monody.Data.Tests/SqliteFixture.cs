using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Monody.Data.Tests;

/// <summary>
/// A real SQLite database held in memory. Kept open for the fixture's lifetime because the
/// database disappears the moment the last connection to it closes.
/// </summary>
public sealed class SqliteFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteFixture()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        Options = new DbContextOptionsBuilder<MonodyDbContext>().UseSqlite(_connection).Options;

        using var db = new MonodyDbContext(Options);
        db.Database.EnsureCreated();
    }

    public DbContextOptions<MonodyDbContext> Options { get; }

    public IDbContextFactory<MonodyDbContext> CreateFactory() => new Factory(Options);

    public void Dispose() => _connection.Dispose();

    private sealed class Factory : IDbContextFactory<MonodyDbContext>
    {
        private readonly DbContextOptions<MonodyDbContext> _options;

        public Factory(DbContextOptions<MonodyDbContext> options) => _options = options;

        public MonodyDbContext CreateDbContext() => new(_options);
    }
}
