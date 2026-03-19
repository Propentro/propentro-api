namespace Propentro.Contracts.Organizations.Roles.Permissions;

public sealed record PermissionItemResponse
{
    public string Code { get; init; } = null!;
    public string? Description { get; init; }
}
