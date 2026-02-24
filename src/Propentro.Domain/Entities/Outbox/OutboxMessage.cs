namespace Propentro.Domain.Entities.Outbox;

/// <summary>
/// Represents a domain event serialized for reliable asynchronous processing.
/// Stored in the outbox_messages table and processed by the OutboxWorkerService.
/// Not an AggregateRoot — has its own lifecycle, no PublicId, no global query filter.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string EventType { get; set; } = default!;
    public string AggregateType { get; set; } = default!;
    public Guid AggregateId { get; set; }
    public string Payload { get; set; } = "{}";
    public string Status { get; set; } = "pending";
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 5;
    public DateTimeOffset? NextRetryAt { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public Guid? RequestId { get; set; }
}
