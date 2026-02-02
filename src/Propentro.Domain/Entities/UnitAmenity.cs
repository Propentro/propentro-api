namespace Propentro.Domain.Entities;

public sealed class UnitAmenity
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public string AmenityCode { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Unit Unit { get; set; } = default!;
    public AmenityType AmenityType { get; set; } = default!;
}
