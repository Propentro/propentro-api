namespace Propentro.Contracts.Organizations.Roles;

public sealed record UpdateRoleRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string[]? PermissionCodes { get; init; }
}
