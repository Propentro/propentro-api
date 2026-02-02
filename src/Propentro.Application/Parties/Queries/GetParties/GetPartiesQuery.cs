using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Parties;

namespace Propentro.Application.Parties.Queries.GetParties;

public sealed record GetPartiesQuery(
    string? Search,
    string? Kind,
    int Page = 1,
    int PageSize = 20
) : IQuery<PagedResult<PartyDto>>;
