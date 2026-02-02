using Propentro.Application.Common.Interfaces;

namespace Propentro.Application.Parties.Commands.CreateParty;

public sealed record CreatePartyCommand(
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
    string? Note
) : ICommand<string>;
