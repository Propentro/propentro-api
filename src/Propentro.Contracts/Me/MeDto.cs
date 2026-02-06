namespace Propentro.Contracts.Me;

public sealed record MeDto(
    string UserId,
    string Email,
    MeOrganizationDto Organization,
    IReadOnlyList<string> Permissions);

public sealed record MeOrganizationDto(
    string OrganizationId,
    string Name,
    string Slug);
