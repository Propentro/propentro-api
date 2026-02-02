namespace Propentro.Domain.Entities;

public sealed class LeaseComplianceDocument
{
    public Guid Id { get; set; }
    public Guid ComplianceId { get; set; }
    public Guid DocumentId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public LeaseCompliance Compliance { get; set; } = default!;
    public Document Document { get; set; } = default!;
}
