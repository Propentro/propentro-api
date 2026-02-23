using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Propentro.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(e => e.EventType).HasColumnName("event_type").IsRequired();
        builder.Property(e => e.AggregateType).HasColumnName("aggregate_type").IsRequired();
        builder.Property(e => e.AggregateId).HasColumnName("aggregate_id").IsRequired();
        builder.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'{}'::jsonb");
        builder.Property(e => e.Status).HasColumnName("status").IsRequired().HasDefaultValue("pending");
        builder.Property(e => e.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
        builder.Property(e => e.MaxRetries).HasColumnName("max_retries").HasDefaultValue(5);
        builder.Property(e => e.NextRetryAt).HasColumnName("next_retry_at");
        builder.Property(e => e.LastError).HasColumnName("last_error");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()");
        builder.Property(e => e.ProcessedAt).HasColumnName("processed_at");
        builder.Property(e => e.CreatedById).HasColumnName("created_by_id");
        builder.Property(e => e.RequestId).HasColumnName("request_id");
    }
}
