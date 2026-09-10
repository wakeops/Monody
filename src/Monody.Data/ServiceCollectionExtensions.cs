using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Monody.Data.Stores;
using Monody.Domain.Extensions;

namespace Monody.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMonodyData(this IServiceCollection services, IConfiguration configuration)
    {
        var options = services.ApplyValidatedOptions<DataOptions>(configuration, "Data");

        services.AddDbContextFactory<MonodyDbContext>(builder => builder.UseSqlite(options.ConnectionString));

        services.AddHostedService<DatabaseMigrationService>();

        services.TryAddSingleton(TimeProvider.System);
        
        services.AddSingleton<IConversationStore, ConversationStore>();
        services.AddSingleton<IMemoryStore, MemoryStore>();
        services.AddSingleton<IReminderStore, ReminderStore>();

        return services;
    }
}
