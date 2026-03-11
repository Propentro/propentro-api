using Propentro.Contracts.Organizations.Settings;

namespace Propentro.Application.Features.Organizations.Settings.Queries.GetOrganizationSettings;

public sealed record GetOrganizationSettingsQuery : ICachedQuery<OrganizationSettingsResponse>
{
    public string CacheKey => "org-settings";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}
