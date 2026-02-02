using Propentro.Domain.Common;

namespace Propentro.Domain.Entities;

public sealed class PropertyCompliance : OrganizationEntity
{
    public Guid PropertyId { get; set; }
    public Guid ComplianceTypeId { get; set; }
    public string Value { get; set; } = default!;
    public DateOnly? IssuedAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
    public string? Note { get; set; }

    // Navigation
    public Property Property { get; set; } = default!;
    public ComplianceType ComplianceType { get; set; } = default!;
    public ICollection<PropertyComplianceDocument> Documents { get; set; } = [];
}
