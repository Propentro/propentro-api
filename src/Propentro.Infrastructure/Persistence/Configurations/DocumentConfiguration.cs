using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Entities;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PublicId).HasColumnName("public_id").IsRequired().ValueGeneratedOnAdd();
        builder.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(e => e.OriginalName).HasColumnName("original_name").IsRequired();
        builder.Property(e => e.MimeType).HasColumnName("mime_type").IsRequired();
        builder.Property(e => e.FileSize).HasColumnName("file_size");
        builder.Property(e => e.StorageKey).HasColumnName("storage_key").IsRequired();
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.DocumentType).HasColumnName("document_type").IsRequired();
        builder.Property(e => e.Width).HasColumnName("width");
        builder.Property(e => e.Height).HasColumnName("height");
        builder.Property(e => e.Status).HasColumnName("status").IsRequired().HasDefaultValue("pending");
        builder.Property(e => e.UploadedById).HasColumnName("uploaded_by_id");
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
    }
}
