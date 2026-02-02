using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Entities;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PublicId).HasColumnName("public_id").IsRequired();
        builder.Property(e => e.OrganizationId).HasColumnName("organization_id");
        builder.Property(e => e.Line1).HasColumnName("line1").IsRequired();
        builder.Property(e => e.Line2).HasColumnName("line2");
        builder.Property(e => e.City).HasColumnName("city").IsRequired();
        builder.Property(e => e.Region).HasColumnName("region");
        builder.Property(e => e.PostalCode).HasColumnName("postal_code");
        builder.Property(e => e.CountryCode).HasColumnName("country_code").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedById).HasColumnName("created_by_id");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedById).HasColumnName("updated_by_id");
        builder.Property(e => e.IsActive).HasColumnName("is_active");

        builder.HasIndex(e => e.PublicId).IsUnique();
    }
}
