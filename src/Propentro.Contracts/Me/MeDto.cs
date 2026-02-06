namespace Propentro.Contracts.Me;

public sealed record MeDto(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    MeOrganizationDto Organization,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record MeOrganizationDto(
    string OrganizationId,
    string Name,
    string Slug,
    string CurrencyCode,
    string Timezone);
