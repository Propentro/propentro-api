using Propentro.Application.Common.Interfaces;

namespace Propentro.Application.Parties.Commands.DeleteParty;

public sealed record DeletePartyCommand(string PublicId) : ICommand;
