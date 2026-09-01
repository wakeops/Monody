using System;
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

    /// <summary>
    /// SQLite has no date type, and EF refuses to order or compare a DateTimeOffset stored as
    /// text. Everything here is UTC, so persist the instant as a number instead; that sorts and
    /// compares correctly in SQL, which the reminder sweep depends on.
    /// </summary>
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
            entity.Property(m => m.Content).IsRequired().HasMaxLength(DataConstants.MaxMemoryLength);
            entity.Property(m => m.Category).HasConversion<string>();
            entity.Property(m => m.CreatedAt).HasConversion(_instantConverter);

            // Every read is "this user's memories", and the unique index makes the store itself
            // reject a duplicate rather than relying on the caller to check first.
            entity.HasIndex(m => m.UserId);
            entity.HasIndex(m => new { m.UserId, m.Category, m.Content }).IsUnique();
        });

        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.Property(r => r.Message).IsRequired().HasMaxLength(DataConstants.MaxReminderLength);
            entity.Property(r => r.DueAt).HasConversion(_instantConverter);
            entity.Property(r => r.CreatedAt).HasConversion(_instantConverter);
            entity.Property(r => r.DeliveredAt).HasConversion(_nullableInstantConverter);

            // The delivery sweep looks for undelivered reminders that are due.
            entity.HasIndex(r => new { r.DeliveredAt, r.DueAt });
            entity.HasIndex(r => r.UserId);
        });
    }
}
