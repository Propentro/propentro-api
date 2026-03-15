using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Organizations;

namespace Propentro.Application.Features.Admin.Organizations.Queries.GetOrganization;

public sealed class GetOrganizationHandler(
    IDbConnectionFactory db) : IQueryHandler<GetOrganizationQuery, AdminOrganizationResponse>
{
    public async Task<Result<AdminOrganizationResponse>> Handle(
        GetOrganizationQuery request, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                o.public_id         AS OrganizationId,
                o.name              AS Name,
                o.slug              AS Slug,
                o.contact_name      AS ContactName,
                o.contact_email     AS ContactEmail,
                o.contact_phone     AS ContactPhone,
                o.currency_code     AS CurrencyCode,
                o.timezone          AS Timezone,
                o.subscription_plan AS SubscriptionPlan,
                o.is_active         AS IsActive,
                COUNT(ou.user_id)   AS MemberCount,
                o.created_at        AS CreatedAt
            FROM organizations o
            LEFT JOIN organization_users ou ON ou.organization_id = o.id
            WHERE o.public_id = @PublicId
            GROUP BY o.id
            """;

        var result = await connection.QueryFirstOrDefaultAsync<AdminOrganizationResponse>(
            sql, new { request.PublicId });

        if (result is null)
            return AdminOrganizationErrors.NotFound;

        return result;
    }
}
