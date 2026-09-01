using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monody.Domain.Extensions;

namespace Monody.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMonodyData(this IServiceCollection services, IConfiguration configuration)
    {
        var options = services.ApplyValidatedOptions<DataOptions>(configuration, "Data");

        // A factory rather than a scoped DbContext: the callers are singletons (plugins, hosted
        // services) with no ambient scope, and each unit of work wants its own short-lived context.
        services.AddDbContextFactory<MonodyDbContext>(builder => builder.UseSqlite(options.ConnectionString));

        services.TryAddTimeProvider();

        services.AddHostedService<DatabaseMigrationService>();

        services.AddSingleton<MemoryStore>();
        services.AddSingleton<ReminderStore>();

        return services;
    }

    private static void TryAddTimeProvider(this IServiceCollection services)
    {
        if (!services.Any(d => d.ServiceType == typeof(TimeProvider)))
        {
            services.AddSingleton(TimeProvider.System);
        }
    }
}
