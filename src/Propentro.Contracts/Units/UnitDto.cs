namespace Propentro.Contracts.Units;

public record UnitDto(
    string PublicId,
    string Name,
    string PropertyPublicId,
    string PropertyName,
    string UnitType,
    string UsageType,
    string Status,
    int? Bedrooms,
    decimal? Bathrooms,
    decimal? AreaSqm,
    string? FurnishedStatus,
    long? DefaultRentMinor,
    string? DefaultCurrency,
    DateTime CreatedAt);
