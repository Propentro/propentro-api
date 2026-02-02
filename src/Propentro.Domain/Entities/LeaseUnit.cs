namespace Propentro.Domain.Entities;

public sealed class LeaseUnit
{
    public Guid LeaseId { get; set; }
    public Guid UnitId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Lease Lease { get; set; } = default!;
    public Unit Unit { get; set; } = default!;
}
