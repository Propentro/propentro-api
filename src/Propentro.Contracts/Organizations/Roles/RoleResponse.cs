namespace Propentro.Contracts.Organizations.Roles;

public sealed record RoleResponse(
    string RoleId,
    string Name,
    string? Description,
    bool IsSystem,
    int MemberCount,
    IReadOnlyList<string> PermissionCodes);
