using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Invites;

namespace Propentro.Application.Features.Organizations.Invites.Queries.GetCurrentOrgInvites;

public sealed record GetCurrentOrgInvitesQuery(
    Guid OrganizationId,
    int Page = 1,
    int PageSize = 25) : IQuery<PagedResult<InviteResponse>>;
