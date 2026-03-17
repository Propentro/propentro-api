namespace Propentro.Contracts.Organizations.Invites;

public sealed record InviteResult(
    bool IsDirectAdd,
    string? MemberPublicId = null,
    string? InvitePublicId = null);
