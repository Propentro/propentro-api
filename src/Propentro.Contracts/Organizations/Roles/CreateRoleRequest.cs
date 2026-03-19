namespace Propentro.Contracts.Organizations.Roles;

public sealed record CreateRoleRequest
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string[] PermissionCodes { get; init; } = [];
}
