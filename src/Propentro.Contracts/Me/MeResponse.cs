namespace Propentro.Contracts.Me;

public sealed record MeResponse
{
    public string UserId { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public MeOrganizationResponse Organization { get; init; } = null!;
    public IReadOnlyList<string> Roles { get; init; } = [];
    public IReadOnlyList<string> Permissions { get; init; } = [];
}

public sealed record MeOrganizationResponse
{
    public string OrganizationId { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
}
