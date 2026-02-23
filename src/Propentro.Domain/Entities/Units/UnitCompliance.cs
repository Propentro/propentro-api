using Propentro.Domain.Common;
using Propentro.Domain.Entities.Lookups;

namespace Propentro.Domain.Entities.Units;

public sealed class UnitCompliance : OrganizationEntity
{
    public const string PublicIdPrefix = "ucm";

    public Guid UnitId { get; set; }
    public Guid ComplianceTypeId { get; set; }
    public string Value { get; set; } = default!;
    public DateOnly? IssuedAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
    public string? Note { get; set; }

    // Navigation
    public Unit Unit { get; set; } = default!;
    public ComplianceType ComplianceType { get; set; } = default!;
    public ICollection<UnitComplianceDocument> Documents { get; set; } = [];
}
