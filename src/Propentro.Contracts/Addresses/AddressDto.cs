namespace Propentro.Contracts.Addresses;

public record AddressDto(
    string PublicId,
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string? PostalCode,
    string CountryCode,
    DateTime CreatedAt);
