using Propentro.Application.Common.Models;
using Propentro.Contracts.Organizations.Members;

namespace Propentro.Application.Features.Organizations.Members.Queries.GetOrganizationMembers;

public sealed record GetOrganizationMembersQuery(int Page, int PageSize, string? Search, string? Status, string? RoleId)
    : IQuery<PagedResult<MemberListItemResponse>>;
