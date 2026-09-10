using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monody.Data.Entities;

namespace Monody.Data.EntityMappings;

public sealed class ConversationMapping : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> entity)
    {
        entity.Property(c => c.Id).ValueGeneratedNever();
        entity.Property(c => c.TurnsJson).IsRequired();
        entity.Property(c => c.CreatedAt).HasConversion(InstantValueConverters.Instant);
        entity.Property(c => c.UpdatedAt).HasConversion(InstantValueConverters.Instant);

        entity.HasIndex(c => c.UpdatedAt);
    }
}
