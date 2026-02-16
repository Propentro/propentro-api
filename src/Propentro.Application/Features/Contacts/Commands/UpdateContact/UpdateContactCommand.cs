using Propentro.Contracts.Contacts;

namespace Propentro.Application.Features.Contacts.Commands.UpdateContact;

public sealed record UpdateContactCommand(
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
    UpdateContactAddressRequest? Address) : ICommand;
