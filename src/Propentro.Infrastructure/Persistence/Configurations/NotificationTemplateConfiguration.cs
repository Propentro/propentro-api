using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Entities;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates");

        builder.HasKey(e => e.Code);

        builder.Property(e => e.Code).HasColumnName("code").IsRequired();
        builder.Property(e => e.Category).HasColumnName("category").IsRequired();
        builder.Property(e => e.TitleTemplate).HasColumnName("title_template").IsRequired();
        builder.Property(e => e.BodyTemplate).HasColumnName("body_template").IsRequired();
        builder.Property(e => e.EmailSubject).HasColumnName("email_subject");
        builder.Property(e => e.EmailBodyHtml).HasColumnName("email_body_html");
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
    }
}
