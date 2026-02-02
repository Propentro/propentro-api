namespace Propentro.Contracts.Properties;

public record CreatePropertyRequest(
    string Name,
    string PropertyKind,
    Guid? AddressId,
    Guid? ParentPropertyId,
    int? YearBuilt,
    decimal? TotalAreaSqm,
    int? TotalFloors,
    string? ReferenceCode,
    string? Note);
