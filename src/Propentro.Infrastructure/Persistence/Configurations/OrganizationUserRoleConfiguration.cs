using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class OrganizationUserRoleConfiguration : IEntityTypeConfiguration<OrganizationUserRole>
{
    public void Configure(EntityTypeBuilder<OrganizationUserRole> builder)
    {
        builder.ToTable("organization_user_roles");

        builder.HasKey(e => new { e.OrganizationId, e.UserId, e.RoleId });

        builder.Property(e => e.OrganizationId).HasColumnName("organization_id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.RoleId).HasColumnName("role_id");
        builder.Property(e => e.AssignedAt).HasColumnName("assigned_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.AssignedBy).HasColumnName("assigned_by");

        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Role)
            .WithMany()
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.AssignedByUser)
            .WithMany()
            .HasForeignKey(e => e.AssignedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
