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
    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? Region { get; init; }
    public string? PostalCode { get; init; }
    public string? CountryCode { get; init; }
    public string? LegalName { get; init; }
    public string? TaxNumber { get; init; }
    public string? TaxCountry { get; init; }
    public bool IsActive { get; init; }
    public int MemberCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
