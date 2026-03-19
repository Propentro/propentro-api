namespace Propentro.Contracts.Admin.Organizations;

public sealed record CreateOrganizationRequest
{
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string CurrencyCode { get; init; } = null!;
    public string Timezone { get; init; } = null!;
}
