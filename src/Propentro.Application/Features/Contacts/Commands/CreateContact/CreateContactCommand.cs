namespace Propentro.Application.Features.Contacts.Commands.CreateContact;

public record CreateContactCommand(
    string Kind,
    string[] Roles,
    string? FirstName,
    string? LastName,
    string? LegalName,
    string? TradingName,
    string? Email,
    string? Phone,
    string? SecondaryEmail,
    string? SecondaryPhone,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Trade,
    string? TradeOther,
    string? TaxNumber,
    string? TaxCountry,
    string? Note,
    CreateContactAddressDto? Address) : ICommand<string>;

public record CreateContactAddressDto(
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string? PostalCode,
    string CountryCode);
