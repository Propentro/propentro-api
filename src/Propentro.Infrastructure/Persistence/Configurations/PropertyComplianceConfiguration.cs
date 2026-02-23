using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class PropertyComplianceConfiguration : IEntityTypeConfiguration<PropertyCompliance>
{
    public void Configure(EntityTypeBuilder<PropertyCompliance> builder)
    {
        builder.ToTable("property_compliance");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PublicId).HasColumnName("public_id").IsRequired().ValueGeneratedOnAdd();
        builder.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(e => e.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(e => e.ComplianceTypeId).HasColumnName("compliance_type_id").IsRequired();
        builder.Property(e => e.Value).HasColumnName("value").IsRequired();
        builder.Property(e => e.IssuedAt).HasColumnName("issued_at");
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at");
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.CreatedById).HasColumnName("created_by_id");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedById).HasColumnName("updated_by_id");

        builder.HasOne(e => e.Property)
            .WithMany(p => p.Compliances)
            .HasForeignKey(e => e.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ComplianceType)
            .WithMany()
            .HasForeignKey(e => e.ComplianceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Documents)
            .WithOne(e => e.Compliance)
            .HasForeignKey(e => e.ComplianceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
