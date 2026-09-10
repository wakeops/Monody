using Microsoft.EntityFrameworkCore;
using Monody.Data.Entities;

namespace Monody.Data;

public class MonodyDbContext : DbContext
{
    public MonodyDbContext(DbContextOptions<MonodyDbContext> options) : base(options)
    {
    }

    public DbSet<UserMemory> UserMemories => Set<UserMemory>();

    public DbSet<Reminder> Reminders => Set<Reminder>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MonodyDbContext).Assembly);
    }
}
