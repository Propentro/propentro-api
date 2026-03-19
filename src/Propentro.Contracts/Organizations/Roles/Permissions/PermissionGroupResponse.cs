namespace Propentro.Contracts.Organizations.Roles.Permissions;

public sealed record PermissionGroupResponse
{
    public string Group { get; init; } = null!;
    public IReadOnlyList<PermissionItemResponse> Permissions { get; init; } = [];
}
