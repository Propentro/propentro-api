using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Entities;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences");

        builder.HasKey(e => new { e.OrganizationId, e.UserId, e.Category });

        builder.Property(e => e.OrganizationId).HasColumnName("organization_id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.Category).HasColumnName("category").IsRequired();
        builder.Property(e => e.EmailEnabled).HasColumnName("email_enabled").HasDefaultValue(true);
        builder.Property(e => e.SmsEnabled).HasColumnName("sms_enabled").HasDefaultValue(false);
        builder.Property(e => e.PushEnabled).HasColumnName("push_enabled").HasDefaultValue(true);
        builder.Property(e => e.InAppEnabled).HasColumnName("in_app_enabled").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
