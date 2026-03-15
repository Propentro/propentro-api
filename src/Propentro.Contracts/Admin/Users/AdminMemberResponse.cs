namespace Propentro.Contracts.Admin.Users;

public sealed record AdminMemberResponse
{
    public string UserId { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string RoleName { get; init; } = null!;
    public string RolePublicId { get; init; } = null!;
    public DateTimeOffset JoinedAt { get; init; }
}
