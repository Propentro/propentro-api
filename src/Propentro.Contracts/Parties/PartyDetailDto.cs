namespace Propentro.Contracts.Parties;

public record PartyDetailDto(
    string PublicId,
    string Kind,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? LegalName,
    string? TradingName,
    string? Email,
    string? Phone,
    string? TaxNumber,
    string? TaxCountry,
    string? Note,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? AddressPublicId,
    string? AddressLine1,
    string? AddressCity,
    string? AddressCountry,
    IReadOnlyList<string> ActiveRoles);
