using Propentro.Domain.Entities.Identity;

namespace Propentro.Domain.Entities.Properties;

public sealed class PropertyType
{
    public Guid Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? Country { get; set; }
    public string Category { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Label { get; set; } = default!;
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation
    public Organization? Organization { get; set; }
}
