using Propentro.Domain.Common;
using Propentro.Domain.Entities.Addresses;
using Propentro.Domain.Entities.Units;

namespace Propentro.Domain.Entities.Properties;

public sealed class Property : OrganizationEntity
{
    public const string PublicIdPrefix = "prp";

    public Guid? ParentPropertyId { get; set; }
    public Guid? AddressId { get; set; }
    public Guid? PropertyTypeId { get; set; }
    public string Name { get; set; } = default!;
    public string? ReferenceCode { get; set; }
    public string Category { get; set; } = default!;
    public string? Purpose { get; set; }
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

    private Property() { }

    public Property(string name, string category) : base(PublicIdPrefix)
    {
        Name = name;
        Category = category;
    }

}
