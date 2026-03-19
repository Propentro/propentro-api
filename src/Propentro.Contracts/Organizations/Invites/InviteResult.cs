namespace Propentro.Contracts.Organizations.Invites;

public sealed record InviteResult
{
    public bool IsDirectAdd { get; init; }
    public string? MemberId { get; init; }
    public string? InviteId { get; init; }
}
