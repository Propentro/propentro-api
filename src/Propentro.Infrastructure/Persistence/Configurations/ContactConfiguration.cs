using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("contacts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PublicId).HasColumnName("public_id").IsRequired().ValueGeneratedOnAdd();
        builder.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(e => e.Kind).HasColumnName("kind").IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").IsRequired();
        builder.Property(e => e.FirstName).HasColumnName("first_name");
        builder.Property(e => e.LastName).HasColumnName("last_name");
        builder.Property(e => e.LegalName).HasColumnName("legal_name");
        builder.Property(e => e.TradingName).HasColumnName("trading_name");
        builder.Property(e => e.Email).HasColumnName("email");
        builder.Property(e => e.Phone).HasColumnName("phone");
        builder.Property(e => e.SecondaryEmail).HasColumnName("secondary_email");
        builder.Property(e => e.SecondaryPhone).HasColumnName("secondary_phone");
        builder.Property(e => e.EmergencyContactName).HasColumnName("emergency_contact_name");
        builder.Property(e => e.EmergencyContactPhone).HasColumnName("emergency_contact_phone");
        builder.Property(e => e.Trade).HasColumnName("trade");
        builder.Property(e => e.TradeOther).HasColumnName("trade_other");
        builder.Property(e => e.AddressId).HasColumnName("address_id");
        builder.Property(e => e.TaxNumber).HasColumnName("tax_number");
        builder.Property(e => e.TaxCountry).HasColumnName("tax_country");
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.IsArchived).HasColumnName("is_archived").HasDefaultValue(false);
        builder.Property(e => e.ArchivedAt).HasColumnName("archived_at");
        builder.Property(e => e.ArchivedById).HasColumnName("archived_by_id");
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.CreatedById).HasColumnName("created_by_id");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedById).HasColumnName("updated_by_id");

        builder.HasOne(e => e.Address)
            .WithMany()
            .HasForeignKey(e => e.AddressId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
