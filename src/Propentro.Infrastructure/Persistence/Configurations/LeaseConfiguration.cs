using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Entities;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> builder)
    {
        builder.ToTable("leases");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PublicId).HasColumnName("public_id").IsRequired().ValueGeneratedOnAdd();
        builder.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(e => e.LeaseType).HasColumnName("lease_type").IsRequired().HasDefaultValue("fixed_term");
        builder.Property(e => e.Status).HasColumnName("status").IsRequired().HasDefaultValue("draft");
        builder.Property(e => e.StartDate).HasColumnName("start_date").IsRequired();
        builder.Property(e => e.EndDate).HasColumnName("end_date");
        builder.Property(e => e.MoveInDate).HasColumnName("move_in_date");
        builder.Property(e => e.MoveOutDate).HasColumnName("move_out_date");
        builder.Property(e => e.ReferenceCode).HasColumnName("reference_code");
        builder.Property(e => e.RentMinor).HasColumnName("rent_minor");
        builder.Property(e => e.Currency).HasColumnName("currency").IsRequired().HasDefaultValue("EUR").HasMaxLength(3).IsFixedLength();
        builder.Property(e => e.RentFrequency).HasColumnName("rent_frequency").IsRequired().HasDefaultValue("monthly");
        builder.Property(e => e.RentDueDay).HasColumnName("rent_due_day");
        builder.Property(e => e.DepositMinor).HasColumnName("deposit_minor");
        builder.Property(e => e.DepositStatus).HasColumnName("deposit_status");
        builder.Property(e => e.DepositHeldBy).HasColumnName("deposit_held_by");
        builder.Property(e => e.DepositReceivedAt).HasColumnName("deposit_received_at");
        builder.Property(e => e.DepositReturnedAt).HasColumnName("deposit_returned_at");
        builder.Property(e => e.DepositReturnMinor).HasColumnName("deposit_return_minor");
        builder.Property(e => e.PreviousLeaseId).HasColumnName("previous_lease_id");
        builder.Property(e => e.NoticePeriodDays).HasColumnName("notice_period_days");
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.CreatedById).HasColumnName("created_by_id");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedById).HasColumnName("updated_by_id");

        builder.HasOne(e => e.PreviousLease)
            .WithMany()
            .HasForeignKey(e => e.PreviousLeaseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.LeaseUnits)
            .WithOne(e => e.Lease)
            .HasForeignKey(e => e.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.LeaseContacts)
            .WithOne(e => e.Lease)
            .HasForeignKey(e => e.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Currency>()
            .WithMany()
            .HasForeignKey(e => e.Currency)
            .HasPrincipalKey(e => e.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
