using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monody.Data.Entities;

namespace Monody.Data.EntityMappings;

public sealed class UserMemoryMapping : IEntityTypeConfiguration<UserMemory>
{
    public void Configure(EntityTypeBuilder<UserMemory> entity)
    {
        entity.Property(m => m.Slug).IsRequired().HasMaxLength(DataConstants.MaxSlugLength);
        entity.Property(m => m.Description).IsRequired().HasMaxLength(DataConstants.MaxMemoryDescriptionLength);
        entity.Property(m => m.Content).IsRequired().HasMaxLength(DataConstants.MaxMemoryContentLength);
        entity.Property(m => m.CreatedAt).HasConversion(InstantValueConverters.Instant);
        entity.Property(m => m.UpdatedAt).HasConversion(InstantValueConverters.Instant);

        entity.HasIndex(m => m.UserId);
        entity.HasIndex(m => new { m.UserId, m.Slug }).IsUnique();
    }
}
