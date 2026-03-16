using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Users;

namespace Propentro.Application.Features.Admin.Users.Queries.GetOrganizationMembers;

public sealed record GetAdminOrganizationMembersQuery(
    string OrganizationId,
    int Page = 1,
    int PageSize = 25) : IQuery<PagedResult<AdminMemberResponse>>;
