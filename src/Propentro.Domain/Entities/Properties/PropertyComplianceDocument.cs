using Propentro.Domain.Entities.Documents;

namespace Propentro.Domain.Entities.Properties;

public sealed class PropertyComplianceDocument
{
    public Guid Id { get; set; }
    public Guid ComplianceId { get; set; }
    public Guid DocumentId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public PropertyCompliance Compliance { get; set; } = default!;
    public Document Document { get; set; } = default!;
}
