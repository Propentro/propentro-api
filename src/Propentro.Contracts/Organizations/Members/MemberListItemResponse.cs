namespace Propentro.Contracts.Organizations.Members;

public sealed record MemberListItemResponse(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string Status,
    DateTimeOffset JoinedAt,
    IReadOnlyList<MemberRoleResponse> Roles);
