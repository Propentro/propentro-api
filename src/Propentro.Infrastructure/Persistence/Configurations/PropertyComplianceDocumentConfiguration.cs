using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class PropertyComplianceDocumentConfiguration : IEntityTypeConfiguration<PropertyComplianceDocument>
{
    public void Configure(EntityTypeBuilder<PropertyComplianceDocument> builder)
    {
        builder.ToTable("property_compliance_documents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ComplianceId).HasColumnName("compliance_id").IsRequired();
        builder.Property(e => e.DocumentId).HasColumnName("document_id").IsRequired();
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(e => e.Compliance)
            .WithMany(e => e.Documents)
            .HasForeignKey(e => e.ComplianceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Document)
            .WithMany()
            .HasForeignKey(e => e.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
