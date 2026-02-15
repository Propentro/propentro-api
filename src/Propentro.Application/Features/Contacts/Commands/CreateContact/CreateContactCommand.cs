using Propentro.Contracts.Contacts;

namespace Propentro.Application.Features.Contacts.Commands.CreateContact;

public sealed record CreateContactCommand(
    string Kind,
    string DisplayName,
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
    CreateContactAddressRequest? Address) : ICommand<string>;
