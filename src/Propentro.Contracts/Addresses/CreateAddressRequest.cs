namespace Propentro.Contracts.Addresses;

public record CreateAddressRequest(
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string? PostalCode,
    string CountryCode);
