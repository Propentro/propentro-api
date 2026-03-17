namespace Propentro.Contracts.Organizations.Invites;

public sealed record InviteResult(
    bool IsDirectAdd,
    string? MemberId = null,
    string? InviteId = null);
