namespace Propentro.Contracts.Properties;

public record PropertyDto(
    string PublicId,
    string Name,
    string PropertyKind,
    int? YearBuilt,
    decimal? TotalAreaSqm,
    int? TotalFloors,
    string? ReferenceCode,
    DateTime CreatedAt,
    string? AddressLine1,
    string? AddressCity,
    string? AddressCountry);
