using Propentro.Contracts.Organizations.Roles;

namespace Propentro.Application.Features.Organizations.Roles.Queries.GetRole;

public sealed record GetRoleQuery(string Id) : IQuery<RoleResponse>;
