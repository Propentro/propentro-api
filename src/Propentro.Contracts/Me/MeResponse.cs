namespace Propentro.Contracts.Me;

public sealed record MeResponse(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    MeOrganizationResponse Organization,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record MeOrganizationResponse(
    string OrganizationId,
    string Name,
    string Slug);
