using Propentro.Domain.Common;

namespace Propentro.Domain.Entities;

public sealed class Property : AggregateRoot
{
    public Guid? ParentPropertyId { get; set; }
    public Guid? AddressId { get; set; }
    public string Name { get; set; } = default!;
    public string? ReferenceCode { get; set; }
    public string PropertyKind { get; set; } = default!;
    public int? YearBuilt { get; set; }
    public decimal? TotalAreaSqm { get; set; }
    public int? TotalFloors { get; set; }
    public string? Note { get; set; }

    // Navigation
    public Property? ParentProperty { get; set; }
    public Address? Address { get; set; }
    public ICollection<Unit> Units { get; set; } = [];
    public ICollection<PropertyContactRole> ContactRoles { get; set; } = [];
    public ICollection<PropertyCompliance> Compliances { get; set; } = [];
    public ICollection<PropertyAmenity> Amenities { get; set; } = [];
    public ICollection<PropertyDocument> Documents { get; set; } = [];
}
