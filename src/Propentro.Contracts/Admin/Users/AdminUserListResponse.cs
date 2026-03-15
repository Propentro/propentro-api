namespace Propentro.Contracts.Admin.Users;

public sealed record AdminUserListResponse
{
    public string UserId { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool IsActive { get; init; }
    public int OrganizationCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
