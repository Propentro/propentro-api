namespace Propentro.Contracts.Admin.Users;

public sealed record AdminMemberResponse
{
    public string UserId { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public IReadOnlyList<MemberRoleInfo> Roles { get; init; } = [];
    public DateTimeOffset JoinedAt { get; init; }
}

public sealed record MemberRoleInfo
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
}
