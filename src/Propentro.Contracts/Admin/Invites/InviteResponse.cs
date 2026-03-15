namespace Propentro.Contracts.Admin.Invites;

public sealed record InviteResponse
{
    public string InviteId { get; init; } = null!;
    public string OrganizationId { get; init; } = null!;
    public string OrganizationName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string RoleName { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string? InvitedBy { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
