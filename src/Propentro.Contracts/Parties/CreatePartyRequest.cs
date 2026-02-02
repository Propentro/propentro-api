namespace Propentro.Contracts.Parties;

public record CreatePartyRequest(
    string Kind,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? LegalName,
    string? TradingName,
    string? Email,
    string? Phone,
    Guid? AddressId,
    string? TaxNumber,
    string? TaxCountry,
    string? Note,
    IReadOnlyList<string>? Roles);
