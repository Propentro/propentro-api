namespace Propentro.Contracts.Properties;

public record UpdatePropertyRequest(
    string? Name,
    string? PropertyKind,
    Guid? AddressId,
    Guid? ParentPropertyId,
    int? YearBuilt,
    decimal? TotalAreaSqm,
    int? TotalFloors,
    string? ReferenceCode,
    string? Note);
