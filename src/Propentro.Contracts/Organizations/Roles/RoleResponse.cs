namespace Propentro.Contracts.Organizations.Roles;

public sealed record RoleResponse
{
    public string RoleId { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public int MemberCount { get; init; }
    public IReadOnlyList<string> PermissionCodes { get; init; } = [];
}
