namespace Propentro.Contracts.Addresses;

public record UpdateAddressRequest(
    string? Line1,
    string? Line2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode);
