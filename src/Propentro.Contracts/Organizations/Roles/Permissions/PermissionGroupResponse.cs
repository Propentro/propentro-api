namespace Propentro.Contracts.Organizations.Roles.Permissions;

public sealed record PermissionGroupResponse(string Group, IReadOnlyList<PermissionItemResponse> Permissions);
