using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class InviteConfiguration : IEntityTypeConfiguration<Invite>
{
    public void Configure(EntityTypeBuilder<Invite> builder)
    {
        builder.ToTable("invites");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PublicId).HasColumnName("public_id").IsRequired().ValueGeneratedOnAdd();
        builder.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(e => e.Email).HasColumnName("email").IsRequired();
        builder.Property(e => e.RoleId).HasColumnName("role_id").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").IsRequired().HasDefaultValue("pending");
        builder.Property(e => e.InvitedById).HasColumnName("invited_by_id");
        builder.Property(e => e.CanceledById).HasColumnName("canceled_by_id");
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Role)
            .WithMany()
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.InvitedByUser)
            .WithMany()
            .HasForeignKey(e => e.InvitedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.CanceledByUser)
            .WithMany()
            .HasForeignKey(e => e.CanceledById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
