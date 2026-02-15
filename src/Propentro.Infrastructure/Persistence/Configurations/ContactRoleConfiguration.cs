using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Entities;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class ContactRoleConfiguration : IEntityTypeConfiguration<ContactRole>
{
    public void Configure(EntityTypeBuilder<ContactRole> builder)
    {
        builder.ToTable("contact_roles");

        builder.HasKey(e => new { e.ContactId, e.Role });

        builder.Property(e => e.ContactId).HasColumnName("contact_id");
        builder.Property(e => e.Role).HasColumnName("role");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");

        builder.HasOne(e => e.Contact)
            .WithMany(e => e.ContactRoles)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
