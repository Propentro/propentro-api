namespace Propentro.Domain.Entities;

public sealed class UnitDocument
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public Guid DocumentId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Unit Unit { get; set; } = default!;
    public Document Document { get; set; } = default!;
}
