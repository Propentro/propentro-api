namespace Propentro.Contracts.MyProfile;

public sealed record MyProfileDto(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    MyOrganizationDto Organization,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record MyOrganizationDto(
    string OrganizationId,
    string Name,
    string Slug);
