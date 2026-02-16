using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Entities;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("units");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PublicId).HasColumnName("public_id").IsRequired().ValueGeneratedOnAdd();
        builder.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(e => e.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(e => e.AddressId).HasColumnName("address_id");
        builder.Property(e => e.ParentUnitId).HasColumnName("parent_unit_id");
        builder.Property(e => e.Name).HasColumnName("name").IsRequired();
        builder.Property(e => e.UsageType).HasColumnName("usage_type").IsRequired().HasDefaultValue("residential");
        builder.Property(e => e.UnitType).HasColumnName("unit_type").IsRequired();
        builder.Property(e => e.OwnershipType).HasColumnName("ownership_type");
        builder.Property(e => e.RentalMode).HasColumnName("rental_mode").IsRequired().HasDefaultValue("long_term");
        builder.Property(e => e.Status).HasColumnName("status").IsRequired().HasDefaultValue("available");
        builder.Property(e => e.Floor).HasColumnName("floor");
        builder.Property(e => e.UnitNumber).HasColumnName("unit_number");
        builder.Property(e => e.BuildingSection).HasColumnName("building_section");
        builder.Property(e => e.Bedrooms).HasColumnName("bedrooms");
        builder.Property(e => e.Bathrooms).HasColumnName("bathrooms").HasPrecision(3, 1);
        builder.Property(e => e.AreaSqm).HasColumnName("area_sqm").HasPrecision(10, 2);
        builder.Property(e => e.FurnishedStatus).HasColumnName("furnished_status");
        builder.Property(e => e.DefaultRentMinor).HasColumnName("default_rent_minor");
        builder.Property(e => e.DefaultDepositMinor).HasColumnName("default_deposit_minor");
        builder.Property(e => e.DefaultCurrency).HasColumnName("default_currency").HasMaxLength(3).IsFixedLength();
        builder.Property(e => e.MonthlyFeesMinor).HasColumnName("monthly_fees_minor");
        builder.Property(e => e.MonthlyFeesNote).HasColumnName("monthly_fees_note");
        builder.Property(e => e.MaxOccupants).HasColumnName("max_occupants");
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.CreatedById).HasColumnName("created_by_id");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedById).HasColumnName("updated_by_id");

        builder.HasOne(e => e.Property)
            .WithMany(e => e.Units)
            .HasForeignKey(e => e.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Address)
            .WithMany()
            .HasForeignKey(e => e.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ParentUnit)
            .WithMany()
            .HasForeignKey(e => e.ParentUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Currency>()
            .WithMany()
            .HasForeignKey(e => e.DefaultCurrency)
            .HasPrincipalKey(e => e.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
