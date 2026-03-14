using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class UnitContactRoleConfiguration : IEntityTypeConfiguration<UnitContactRole>
{
    public void Configure(EntityTypeBuilder<UnitContactRole> builder)
    {
        builder.ToTable("unit_contact_roles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(e => e.UnitId).HasColumnName("unit_id").IsRequired();
        builder.Property(e => e.ContactId).HasColumnName("contact_id").IsRequired();
        builder.Property(e => e.Role).HasColumnName("role").IsRequired();
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
        builder.Property(e => e.OwnershipPct).HasColumnName("ownership_pct").HasPrecision(5, 2);
        builder.Property(e => e.ValidFrom).HasColumnName("valid_from");
        builder.Property(e => e.ValidTo).HasColumnName("valid_to");
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.CreatedById).HasColumnName("created_by_id");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedById).HasColumnName("updated_by_id");

        builder.HasOne(e => e.Unit)
            .WithMany(u => u.ContactRoles)
            .HasForeignKey(e => e.UnitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Contact)
            .WithMany()
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
