using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Entities;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class ComplianceTypeConfiguration : IEntityTypeConfiguration<ComplianceType>
{
    public void Configure(EntityTypeBuilder<ComplianceType> builder)
    {
        builder.ToTable("compliance_types");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Code).HasColumnName("code").IsRequired();
        builder.Property(e => e.Target).HasColumnName("target").IsRequired();
        builder.Property(e => e.Country).HasColumnName("country").IsRequired();
        builder.Property(e => e.Label).HasColumnName("label").IsRequired();
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
    }
}
