using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Monody.Data;

/// <summary>
/// Applies pending migrations at startup, before anything can read or write.
/// </summary>
internal sealed class DatabaseMigrationService : IHostedService
{
    private readonly IDbContextFactory<MonodyDbContext> _dbContextFactory;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(IDbContextFactory<MonodyDbContext> dbContextFactory, ILogger<DatabaseMigrationService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogInformation("Applying database migrations to {DataSource}.", db.Database.GetDbConnection().DataSource);

        await db.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
