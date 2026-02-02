using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Units;

namespace Propentro.Application.Units.Queries.GetUnits;

public sealed record GetUnitsQuery(
    Guid? PropertyId,
    string? Search,
    string? Status,
    int Page = 1,
    int PageSize = 20
) : IQuery<PagedResult<UnitDto>>;
