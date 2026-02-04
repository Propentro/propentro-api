
namespace Propentro.Application.Parties.Commands.DeleteParty;

public sealed record DeletePartyCommand(string PublicId) : ICommand;
