using Propentro.Domain.Common;

namespace Propentro.Domain.Entities;

public sealed class LeaseCompliance : OrganizationEntity
{
    public Guid LeaseId { get; set; }
    public Guid ComplianceTypeId { get; set; }
    public string Value { get; set; } = default!;
    public DateOnly? IssuedAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
    public string? Note { get; set; }

    // Navigation
    public Lease Lease { get; set; } = default!;
    public ComplianceType ComplianceType { get; set; } = default!;
    public ICollection<LeaseComplianceDocument> Documents { get; set; } = [];
}
