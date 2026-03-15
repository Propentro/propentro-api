namespace Propentro.Contracts.Admin.Users;

public sealed record AdminUserResponse
{
    public string UserId { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public List<UserOrganizationInfo> Organizations { get; init; } = [];
}

public sealed record UserOrganizationInfo
{
    public string OrganizationId { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string RoleName { get; init; } = null!;
}
