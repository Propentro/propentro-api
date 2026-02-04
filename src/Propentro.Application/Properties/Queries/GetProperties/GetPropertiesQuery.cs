using Propentro.Application.Common.Models;
using Propentro.Contracts.Properties;

namespace Propentro.Application.Properties.Queries.GetProperties;

public sealed record GetPropertiesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? PropertyKind = null
) : IQuery<PagedResult<PropertyDto>>;
