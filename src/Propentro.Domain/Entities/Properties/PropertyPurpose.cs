namespace Propentro.Domain.Entities.Properties;

public sealed class PropertyPurpose
{
    public string Code { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
