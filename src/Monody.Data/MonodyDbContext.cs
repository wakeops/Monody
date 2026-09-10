using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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

    private static readonly ValueConverter<DateTimeOffset, long> _instantConverter = new(
        value => value.ToUnixTimeMilliseconds(),
        value => DateTimeOffset.FromUnixTimeMilliseconds(value));

    private static readonly ValueConverter<DateTimeOffset?, long?> _nullableInstantConverter = new(
        value => value.HasValue ? value.Value.ToUnixTimeMilliseconds() : null,
        value => value.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(value.Value) : null);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserMemory>(entity =>
        {
            entity.Property(m => m.Slug).IsRequired().HasMaxLength(DataConstants.MaxSlugLength);
            entity.Property(m => m.Description).IsRequired().HasMaxLength(DataConstants.MaxMemoryDescriptionLength);
            entity.Property(m => m.Content).IsRequired().HasMaxLength(DataConstants.MaxMemoryContentLength);
            entity.Property(m => m.CreatedAt).HasConversion(_instantConverter);
            entity.Property(m => m.UpdatedAt).HasConversion(_instantConverter);

            entity.HasIndex(m => m.UserId);
            entity.HasIndex(m => new { m.UserId, m.Slug }).IsUnique();
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.Property(c => c.Id).ValueGeneratedNever();
            entity.Property(c => c.TurnsJson).IsRequired();
            entity.Property(c => c.CreatedAt).HasConversion(_instantConverter);
            entity.Property(c => c.UpdatedAt).HasConversion(_instantConverter);

            entity.HasIndex(c => c.UpdatedAt);
        });

        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.Property(r => r.Message).IsRequired().HasMaxLength(DataConstants.MaxReminderLength);
            entity.Property(r => r.DueAt).HasConversion(_instantConverter);
            entity.Property(r => r.CreatedAt).HasConversion(_instantConverter);
            entity.Property(r => r.DeliveredAt).HasConversion(_nullableInstantConverter);

            entity.HasIndex(r => new { r.DeliveredAt, r.DueAt });
            entity.HasIndex(r => r.UserId);
        });
    }
}
