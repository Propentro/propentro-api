namespace Propentro.Contracts.Admin.Organizations;

public sealed record UpdateOrganizationRequest
{
    public string Name { get; init; } = null!;
    public string? ContactName { get; init; }
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public string CurrencyCode { get; init; } = null!;
    public string Timezone { get; init; } = null!;
}
