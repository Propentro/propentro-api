namespace Propentro.Contracts.Admin.Organizations;

public sealed record AdminOrganizationResponse
{
    public string OrganizationId { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string? ContactName { get; init; }
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public string CurrencyCode { get; init; } = null!;
    public string Timezone { get; init; } = null!;
    public bool IsActive { get; init; }
    public int MemberCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
