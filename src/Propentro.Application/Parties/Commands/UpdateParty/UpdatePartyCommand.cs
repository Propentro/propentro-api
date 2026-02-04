
namespace Propentro.Application.Parties.Commands.UpdateParty;

public sealed record UpdatePartyCommand(
    string PublicId,
    string? DisplayName,
    string? FirstName,
    string? LastName,
    string? LegalName,
    string? TradingName,
    string? Email,
    string? Phone,
    string? TaxNumber,
    string? TaxCountry,
    string? Note
) : ICommand;
