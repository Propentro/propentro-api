using Propentro.Contracts.Admin.Users;

namespace Propentro.Application.Features.Admin.Users.Queries.GetOrganizationMembers;

public sealed record GetAdminOrganizationMembersQuery(string OrganizationId) : IQuery<List<AdminMemberResponse>>;
