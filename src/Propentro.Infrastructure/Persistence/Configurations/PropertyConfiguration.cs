using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Entities;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("properties");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PublicId).HasColumnName("public_id").IsRequired().ValueGeneratedOnAdd();
        builder.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(e => e.ParentPropertyId).HasColumnName("parent_property_id");
        builder.Property(e => e.AddressId).HasColumnName("address_id");
        builder.Property(e => e.Name).HasColumnName("name").IsRequired();
        builder.Property(e => e.ReferenceCode).HasColumnName("reference_code");
        builder.Property(e => e.PropertyKind).HasColumnName("property_kind").IsRequired();
        builder.Property(e => e.YearBuilt).HasColumnName("year_built");
        builder.Property(e => e.TotalAreaSqm).HasColumnName("total_area_sqm").HasPrecision(12, 2);
        builder.Property(e => e.TotalFloors).HasColumnName("total_floors");
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedById).HasColumnName("created_by_id");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedById).HasColumnName("updated_by_id");

        builder.HasOne(e => e.ParentProperty)
            .WithMany()
            .HasForeignKey(e => e.ParentPropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Address)
            .WithMany()
            .HasForeignKey(e => e.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Units)
            .WithOne(e => e.Property)
            .HasForeignKey(e => e.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
