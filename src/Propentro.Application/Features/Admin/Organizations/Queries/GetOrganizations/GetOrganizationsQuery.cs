using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Organizations;

namespace Propentro.Application.Features.Admin.Organizations.Queries.GetOrganizations;

public sealed record GetOrganizationsQuery(
    string? Search,
    bool? IsActive,
    int Page = 1,
    int PageSize = 25) : IQuery<PagedResult<AdminOrganizationListResponse>>;
