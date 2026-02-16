using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Entities;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class LeaseDocumentConfiguration : IEntityTypeConfiguration<LeaseDocument>
{
    public void Configure(EntityTypeBuilder<LeaseDocument> builder)
    {
        builder.ToTable("lease_documents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.LeaseId).HasColumnName("lease_id").IsRequired();
        builder.Property(e => e.DocumentId).HasColumnName("document_id").IsRequired();
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(e => e.Lease)
            .WithMany()
            .HasForeignKey(e => e.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Document)
            .WithMany()
            .HasForeignKey(e => e.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
