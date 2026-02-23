using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("countries");

        builder.HasKey(e => e.Code);

        builder.Property(e => e.Code).HasColumnName("code").IsRequired().HasMaxLength(2).IsFixedLength();
        builder.Property(e => e.Name).HasColumnName("name").IsRequired();
        builder.Property(e => e.DialCode).HasColumnName("dial_code");
        builder.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsFixedLength();
        builder.Property(e => e.RegionLabel).HasColumnName("region_label");
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(e => e.Currency)
            .WithMany()
            .HasForeignKey(e => e.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
