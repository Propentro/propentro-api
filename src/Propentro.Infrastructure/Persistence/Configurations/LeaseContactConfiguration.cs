using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Entities;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class LeaseContactConfiguration : IEntityTypeConfiguration<LeaseContact>
{
    public void Configure(EntityTypeBuilder<LeaseContact> builder)
    {
        builder.ToTable("lease_contacts");

        builder.HasKey(e => new { e.LeaseId, e.ContactId, e.Role });

        builder.Property(e => e.LeaseId).HasColumnName("lease_id");
        builder.Property(e => e.ContactId).HasColumnName("contact_id");
        builder.Property(e => e.Role).HasColumnName("role").IsRequired();
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
        builder.Property(e => e.IsSignatory).HasColumnName("is_signatory").HasDefaultValue(true);
        builder.Property(e => e.IsOccupant).HasColumnName("is_occupant").HasDefaultValue(false);
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");

        builder.HasOne(e => e.Lease)
            .WithMany(e => e.LeaseContacts)
            .HasForeignKey(e => e.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Contact)
            .WithMany()
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
