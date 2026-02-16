using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Entities;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class AmenityTypeConfiguration : IEntityTypeConfiguration<AmenityType>
{
    public void Configure(EntityTypeBuilder<AmenityType> builder)
    {
        builder.ToTable("amenity_types");

        builder.HasKey(e => e.Code);

        builder.Property(e => e.Code).HasColumnName("code").IsRequired();
        builder.Property(e => e.Target).HasColumnName("target");
        builder.Property(e => e.Label).HasColumnName("label").IsRequired();
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
    }
}
