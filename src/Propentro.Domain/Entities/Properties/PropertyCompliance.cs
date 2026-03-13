using Propentro.Domain.Common;
using Propentro.Domain.Entities.Lookups;

namespace Propentro.Domain.Entities.Properties;

public sealed class PropertyCompliance : OrganizationEntity
{
    public const string PublicIdPrefix = "prc";

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
