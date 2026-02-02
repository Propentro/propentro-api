using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Entities;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class LeaseUnitConfiguration : IEntityTypeConfiguration<LeaseUnit>
{
    public void Configure(EntityTypeBuilder<LeaseUnit> builder)
    {
        builder.ToTable("lease_units");

        builder.HasKey(e => new { e.LeaseId, e.UnitId });

        builder.Property(e => e.LeaseId).HasColumnName("lease_id");
        builder.Property(e => e.UnitId).HasColumnName("unit_id");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");

        builder.HasOne(e => e.Lease)
            .WithMany(e => e.LeaseUnits)
            .HasForeignKey(e => e.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Unit)
            .WithMany(e => e.LeaseUnits)
            .HasForeignKey(e => e.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
