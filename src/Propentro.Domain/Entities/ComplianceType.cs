namespace Propentro.Domain.Entities;

public sealed class ComplianceType
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Target { get; set; } = default!;
    public string Country { get; set; } = default!;
    public string Label { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
