using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Entities;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PublicId).HasColumnName("public_id").IsRequired().ValueGeneratedOnAdd();
        builder.Property(e => e.Slug).HasColumnName("slug").IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").IsRequired();
        builder.Property(e => e.ExternalId).HasColumnName("external_id");
        builder.Property(e => e.AuthProvider).HasColumnName("auth_provider");
        builder.Property(e => e.ContactName).HasColumnName("contact_name");
        builder.Property(e => e.ContactEmail).HasColumnName("contact_email");
        builder.Property(e => e.ContactPhone).HasColumnName("contact_phone");
        builder.Property(e => e.AddressLine1).HasColumnName("address_line1");
        builder.Property(e => e.AddressLine2).HasColumnName("address_line2");
        builder.Property(e => e.City).HasColumnName("city");
        builder.Property(e => e.Region).HasColumnName("region");
        builder.Property(e => e.PostalCode).HasColumnName("postal_code");
        builder.Property(e => e.CountryCode).HasColumnName("country_code");
        builder.Property(e => e.LegalName).HasColumnName("legal_name");
        builder.Property(e => e.TaxNumber).HasColumnName("tax_number");
        builder.Property(e => e.TaxCountry).HasColumnName("tax_country");
        builder.Property(e => e.CurrencyCode).HasColumnName("currency_code").IsRequired().HasDefaultValue("EUR");
        builder.Property(e => e.Timezone).HasColumnName("timezone").IsRequired().HasDefaultValue("UTC");
        builder.Property(e => e.SubscriptionPlan).HasColumnName("subscription_plan").IsRequired().HasDefaultValue("starter");
        builder.Property(e => e.UiSettings).HasColumnName("ui_settings").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        builder.Property(e => e.NotificationSettings).HasColumnName("notification_settings").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.CreatedById).HasColumnName("created_by_id");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedById).HasColumnName("updated_by_id");
    }
}
