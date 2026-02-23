namespace Propentro.Application.Features.Contacts.Commands.UpdateContact;

public record UpdateContactCommand(
    string ContactId,
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
    UpdateContactAddressDto? Address) : ICommand;

public record UpdateContactAddressDto(
    string? Line1,
    string? Line2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode);
