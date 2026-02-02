namespace Propentro.Domain.Entities;

public sealed class UnitComplianceDocument
{
    public Guid Id { get; set; }
    public Guid ComplianceId { get; set; }
    public Guid DocumentId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public UnitCompliance Compliance { get; set; } = default!;
    public Document Document { get; set; } = default!;
}
