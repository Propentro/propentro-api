using Propentro.Contracts.Parties;

namespace Propentro.Application.Parties.Queries.GetPartyById;

public sealed record GetPartyByIdQuery(string PublicId) : IQuery<PartyDetailDto>;
