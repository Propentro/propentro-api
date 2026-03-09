namespace Propentro.Contracts.Organizations.Roles;

public sealed record UpdateRoleRequest(string? Name, string? Description, string[]? PermissionCodes);
