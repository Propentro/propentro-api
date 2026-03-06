using Dapper;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Organizations.Settings;

namespace Propentro.Application.Features.Organizations.Settings.Queries.GetOrganizationSettings;

public sealed class GetOrganizationSettingsHandler(
    IDbConnectionFactory db,
    IUserContext currentUser) : IQueryHandler<GetOrganizationSettingsQuery, OrganizationSettingsResponse>
{
    public async Task<Result<OrganizationSettingsResponse>> Handle(
        GetOrganizationSettingsQuery request,
        CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                o.public_id       AS OrganizationId,
                o.name            AS Name,
                o.slug            AS Slug,
                o.contact_name    AS ContactName,
                o.contact_email   AS ContactEmail,
                o.contact_phone   AS ContactPhone,
                o.address_line1   AS AddressLine1,
                o.address_line2   AS AddressLine2,
                o.city            AS City,
                o.region          AS Region,
                o.postal_code     AS PostalCode,
                o.country_code    AS CountryCode,
                o.legal_name      AS LegalName,
                o.tax_number      AS TaxNumber,
                o.tax_country     AS TaxCountry,
                o.currency_code   AS CurrencyCode,
                o.timezone        AS Timezone
            FROM organizations o
            WHERE o.id = @OrganizationId
              AND o.is_active = true
            """;

        var result = await connection.QueryFirstOrDefaultAsync<OrganizationSettingsQueryResult>(
            sql,
            new { currentUser.OrganizationId });

        if (result is null)
            return OrganizationSettingsErrors.NotFound;

        return new OrganizationSettingsResponse(
            result.OrganizationId,
            result.Name,
            result.Slug,
            result.ContactName,
            result.ContactEmail,
            result.ContactPhone,
            result.AddressLine1,
            result.AddressLine2,
            result.City,
            result.Region,
            result.PostalCode,
            result.CountryCode,
            result.LegalName,
            result.TaxNumber,
            result.TaxCountry,
            result.CurrencyCode,
            result.Timezone);
    }

    private sealed class OrganizationSettingsQueryResult
    {
        public string OrganizationId { get; init; } = null!;
        public string Name { get; init; } = null!;
        public string Slug { get; init; } = null!;
        public string? ContactName { get; init; }
        public string? ContactEmail { get; init; }
        public string? ContactPhone { get; init; }
        public string? AddressLine1 { get; init; }
        public string? AddressLine2 { get; init; }
        public string? City { get; init; }
        public string? Region { get; init; }
        public string? PostalCode { get; init; }
        public string? CountryCode { get; init; }
        public string? LegalName { get; init; }
        public string? TaxNumber { get; init; }
        public string? TaxCountry { get; init; }
        public string CurrencyCode { get; init; } = null!;
        public string Timezone { get; init; } = null!;
    }
}
