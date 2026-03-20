namespace Propentro.Contracts.Admin.Organizations;

public sealed record AdminOrgRoleResponse
{
    public string RoleId { get; init; } = null!;
    public string Name { get; init; } = null!;
    public bool IsSystem { get; init; }
}
