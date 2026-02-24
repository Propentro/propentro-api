using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("document_versions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PublicId).HasColumnName("public_id").IsRequired().ValueGeneratedOnAdd();
        builder.Property(e => e.DocumentId).HasColumnName("document_id").IsRequired();
        builder.Property(e => e.OriginalName).HasColumnName("original_name").IsRequired();
        builder.Property(e => e.MimeType).HasColumnName("mime_type").IsRequired();
        builder.Property(e => e.FileSize).HasColumnName("file_size");
        builder.Property(e => e.StorageKey).HasColumnName("storage_key").IsRequired();
        builder.Property(e => e.Width).HasColumnName("width");
        builder.Property(e => e.Height).HasColumnName("height");
        builder.Property(e => e.VersionNumber).HasColumnName("version_number").IsRequired().HasDefaultValue(1);
        builder.Property(e => e.Status).HasColumnName("status").IsRequired().HasDefaultValue("pending");
        builder.Property(e => e.UploadedById).HasColumnName("uploaded_by_id");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
