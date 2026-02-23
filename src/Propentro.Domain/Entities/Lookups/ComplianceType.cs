namespace Propentro.Domain.Entities.Lookups;

public sealed class ComplianceType
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Target { get; set; } = default!;
    public string Country { get; set; } = default!;
    public string Label { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
