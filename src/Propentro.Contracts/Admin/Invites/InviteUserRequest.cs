namespace Propentro.Contracts.Admin.Invites;

public sealed record InviteUserRequest
{
    public string Email { get; init; } = null!;
    public string RoleId { get; init; } = null!;
}
