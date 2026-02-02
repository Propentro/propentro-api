using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Entities;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class LeasePartyConfiguration : IEntityTypeConfiguration<LeaseParty>
{
    public void Configure(EntityTypeBuilder<LeaseParty> builder)
    {
        builder.ToTable("lease_parties");

        builder.HasKey(e => new { e.LeaseId, e.PartyId });
        builder.Property(e => e.LeaseId).HasColumnName("lease_id");
        builder.Property(e => e.PartyId).HasColumnName("party_id");
        builder.Property(e => e.Role).HasColumnName("role");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.Property(e => e.IsSignatory).HasColumnName("is_signatory");
        builder.Property(e => e.IsOccupant).HasColumnName("is_occupant");
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");

        builder.HasOne(e => e.Lease)
            .WithMany(e => e.LeaseParties)
            .HasForeignKey(e => e.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Party)
            .WithMany()
            .HasForeignKey(e => e.PartyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
