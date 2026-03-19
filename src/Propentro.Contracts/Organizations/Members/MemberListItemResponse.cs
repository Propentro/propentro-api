namespace Propentro.Contracts.Organizations.Members;

public sealed record MemberListItemResponse
{
    public string UserId { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string Status { get; init; } = null!;
    public DateTimeOffset JoinedAt { get; init; }
    public IReadOnlyList<MemberRoleResponse> Roles { get; init; } = [];
}
