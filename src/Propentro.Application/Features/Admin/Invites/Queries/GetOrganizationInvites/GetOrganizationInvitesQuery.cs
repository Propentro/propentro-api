using Propentro.Contracts.Admin.Invites;

namespace Propentro.Application.Features.Admin.Invites.Queries.GetOrganizationInvites;

public sealed record GetOrganizationInvitesQuery(string OrganizationId) : IQuery<List<InviteResponse>>;
