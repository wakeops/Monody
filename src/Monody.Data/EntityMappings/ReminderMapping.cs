using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monody.Data.Entities;

namespace Monody.Data.EntityMappings;

public sealed class ReminderMapping : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> entity)
    {
        entity.Property(r => r.Message).IsRequired().HasMaxLength(DataConstants.MaxReminderLength);
        entity.Property(r => r.DueAt).HasConversion(InstantValueConverters.Instant);
        entity.Property(r => r.CreatedAt).HasConversion(InstantValueConverters.Instant);
        entity.Property(r => r.DeliveredAt).HasConversion(InstantValueConverters.NullableInstant);

        entity.HasIndex(r => new { r.DeliveredAt, r.DueAt });
        entity.HasIndex(r => r.UserId);
    }
}
