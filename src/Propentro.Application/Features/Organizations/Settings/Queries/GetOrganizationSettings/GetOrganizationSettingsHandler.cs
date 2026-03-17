using Dapper;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Errors;
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
                o.public_id       AS organization_id,
                o.name            AS name,
                o.slug            AS slug,
                o.contact_name    AS contact_name,
                o.contact_email   AS contact_email,
                o.contact_phone   AS contact_phone,
                o.address_line1   AS address_line1,
                o.address_line2   AS address_line2,
                o.city            AS city,
                o.region          AS region,
                o.postal_code     AS postal_code,
                o.country_code    AS country_code,
                o.legal_name      AS legal_name,
                o.tax_number      AS tax_number,
                o.tax_country     AS tax_country,
                o.currency_code   AS currency_code,
                o.timezone        AS timezone
            FROM organizations o
            WHERE o.id = @OrganizationId
              AND o.is_active = true
            """;

        var result = await connection.QueryFirstOrDefaultAsync<OrganizationSettingsResponse>(
            sql,
            new { currentUser.OrganizationId });

        if (result is null)
            return OrganizationErrors.NotFound;

        return result;
    }
}
