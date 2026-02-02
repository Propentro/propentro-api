using Propentro.Domain.Common;

namespace Propentro.Domain.Entities;

public sealed class Unit : AggregateRoot
{
    public Guid PropertyId { get; set; }
    public Guid? AddressId { get; set; }
    public Guid? ParentUnitId { get; set; }
    public string Name { get; set; } = default!;
    public string UsageType { get; set; } = "residential";
    public string UnitType { get; set; } = default!;
    public string? OwnershipType { get; set; }
    public string RentalMode { get; set; } = "long_term";
    public string Status { get; set; } = "available";

    // Location within property
    public int? Floor { get; set; }
    public string? UnitNumber { get; set; }
    public string? BuildingSection { get; set; }

    // Specs
    public int? Bedrooms { get; set; }
    public decimal? Bathrooms { get; set; }
    public decimal? AreaSqm { get; set; }
    public string? FurnishedStatus { get; set; }

    // Rent defaults (minor units)
    public long? DefaultRentMinor { get; set; }
    public long? DefaultDepositMinor { get; set; }
    public string? DefaultCurrency { get; set; }

    // Recurring fees
    public long? MonthlyFeesMinor { get; set; }
    public string? MonthlyFeesNote { get; set; }

    // Policies
    public int? MaxOccupants { get; set; }

    public string? Note { get; set; }

    // Navigation
    public Property Property { get; set; } = default!;
    public Address? Address { get; set; }
    public Unit? ParentUnit { get; set; }
    public ICollection<UnitPartyRole> PartyRoles { get; set; } = [];
    public ICollection<UnitCompliance> Compliances { get; set; } = [];
    public ICollection<UnitAmenity> Amenities { get; set; } = [];
    public ICollection<UnitDocument> Documents { get; set; } = [];
    public ICollection<LeaseUnit> LeaseUnits { get; set; } = [];
}
