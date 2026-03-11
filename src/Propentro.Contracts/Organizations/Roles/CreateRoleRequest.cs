namespace Propentro.Contracts.Organizations.Roles;

public sealed record CreateRoleRequest(string Name, string? Description, string[] PermissionCodes);
