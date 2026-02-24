namespace Propentro.Domain.Entities.Lookups;

public sealed class AmenityType
{
    public string Code { get; set; } = default!;
    public string? Target { get; set; }
    public string Label { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
