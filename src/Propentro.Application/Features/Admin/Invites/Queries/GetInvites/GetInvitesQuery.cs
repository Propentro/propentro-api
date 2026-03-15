using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Invites;

namespace Propentro.Application.Features.Admin.Invites.Queries.GetInvites;

public sealed record GetInvitesQuery(
    string? Status,
    int Page = 1,
    int PageSize = 25) : IQuery<PagedResult<InviteResponse>>;
