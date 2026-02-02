namespace Propentro.Domain.Entities;

public sealed class PropertyAmenity
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string AmenityCode { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Property Property { get; set; } = default!;
    public AmenityType AmenityType { get; set; } = default!;
}
