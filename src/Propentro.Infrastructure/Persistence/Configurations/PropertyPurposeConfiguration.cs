using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class PropertyPurposeConfiguration : IEntityTypeConfiguration<PropertyPurpose>
{
    public void Configure(EntityTypeBuilder<PropertyPurpose> builder)
    {
        builder.ToTable("property_purposes");

        builder.HasKey(e => e.Code);

        builder.Property(e => e.Code).HasColumnName("code").IsRequired();
        builder.Property(e => e.Label).HasColumnName("label").IsRequired();
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
    }
}
