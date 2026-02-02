namespace Propentro.Contracts.Properties;

public record PropertyDetailDto(
    string PublicId,
    string Name,
    string PropertyKind,
    int? YearBuilt,
    decimal? TotalAreaSqm,
    int? TotalFloors,
    string? ReferenceCode,
    string? Note,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? AddressPublicId,
    string? AddressLine1,
    string? AddressLine2,
    string? AddressCity,
    string? AddressRegion,
    string? AddressPostalCode,
    string? AddressCountry,
    string? ParentPropertyPublicId,
    string? ParentPropertyName);
