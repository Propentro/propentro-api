namespace Propentro.Contracts.Lookups;

public sealed record CountryResponse(
    string Code,
    string Name,
    string? DialCode,
    string? CurrencyCode,
    string? RegionLabel);
