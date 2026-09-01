using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Monody.Data;

/// <summary>
/// Used only by `dotnet ef` when scaffolding migrations. The runtime builds its context from
/// configuration instead; the connection string here is never opened.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MonodyDbContext>
{
    public MonodyDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<MonodyDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
