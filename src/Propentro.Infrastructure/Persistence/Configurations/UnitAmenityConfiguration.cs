using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class UnitAmenityConfiguration : IEntityTypeConfiguration<UnitAmenity>
{
    public void Configure(EntityTypeBuilder<UnitAmenity> builder)
    {
        builder.ToTable("unit_amenities");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.UnitId).HasColumnName("unit_id").IsRequired();
        builder.Property(e => e.AmenityCode).HasColumnName("amenity_code").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(e => e.Unit)
            .WithMany()
            .HasForeignKey(e => e.UnitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.AmenityType)
            .WithMany()
            .HasForeignKey(e => e.AmenityCode)
            .HasPrincipalKey(e => e.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
