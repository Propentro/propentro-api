using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class PropertyAmenityConfiguration : IEntityTypeConfiguration<PropertyAmenity>
{
    public void Configure(EntityTypeBuilder<PropertyAmenity> builder)
    {
        builder.ToTable("property_amenities");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(e => e.AmenityCode).HasColumnName("amenity_code").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(e => e.Property)
            .WithMany(p => p.Amenities)
            .HasForeignKey(e => e.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.AmenityType)
            .WithMany()
            .HasForeignKey(e => e.AmenityCode)
            .HasPrincipalKey(e => e.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
