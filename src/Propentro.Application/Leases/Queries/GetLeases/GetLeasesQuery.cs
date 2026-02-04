using Propentro.Application.Common.Models;
using Propentro.Contracts.Leases;

namespace Propentro.Application.Leases.Queries.GetLeases;

public sealed record GetLeasesQuery(
    string? Status,
    Guid? UnitId,
    int Page = 1,
    int PageSize = 20
) : IQuery<PagedResult<LeaseDto>>;
