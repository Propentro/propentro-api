using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Entities.Tags;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class EntityTagConfiguration : IEntityTypeConfiguration<EntityTag>
{
    public void Configure(EntityTypeBuilder<EntityTag> builder)
    {
        builder.ToTable("entity_tags");

        builder.HasKey(e => new { e.TagId, e.EntityType, e.EntityId });

        builder.Property(e => e.TagId).HasColumnName("tag_id");
        builder.Property(e => e.EntityType).HasColumnName("entity_type").IsRequired().HasMaxLength(50);
        builder.Property(e => e.EntityId).HasColumnName("entity_id");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.CreatedById).HasColumnName("created_by_id");

        builder.HasOne(e => e.Tag)
            .WithMany()
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
