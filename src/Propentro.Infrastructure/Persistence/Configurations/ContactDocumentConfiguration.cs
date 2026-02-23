using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class ContactDocumentConfiguration : IEntityTypeConfiguration<ContactDocument>
{
    public void Configure(EntityTypeBuilder<ContactDocument> builder)
    {
        builder.ToTable("contact_documents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ContactId).HasColumnName("contact_id").IsRequired();
        builder.Property(e => e.DocumentId).HasColumnName("document_id").IsRequired();
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(e => e.Contact)
            .WithMany()
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Document)
            .WithMany()
            .HasForeignKey(e => e.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
