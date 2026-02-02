namespace Propentro.Domain.Entities;

public sealed class LeaseDocument
{
    public Guid Id { get; set; }
    public Guid LeaseId { get; set; }
    public Guid DocumentId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Lease Lease { get; set; } = default!;
    public Document Document { get; set; } = default!;
}
