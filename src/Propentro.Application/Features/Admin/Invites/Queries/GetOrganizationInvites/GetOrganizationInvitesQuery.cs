using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Invites;

namespace Propentro.Application.Features.Admin.Invites.Queries.GetOrganizationInvites;

public sealed record GetOrganizationInvitesQuery(
    string OrganizationId,
    int Page = 1,
    int PageSize = 25) : IQuery<PagedResult<InviteResponse>>;
