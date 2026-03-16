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
                o.public_id         AS organization_id,
                o.name              AS name,
                o.slug              AS slug,
                o.contact_name      AS contact_name,
                o.contact_email     AS contact_email,
                o.contact_phone     AS contact_phone,
                o.currency_code     AS currency_code,
                o.timezone          AS timezone,
                o.subscription_plan AS subscription_plan,
                o.is_active         AS is_active,
                COUNT(ou.user_id)   AS member_count,
                o.created_at        AS created_at
            FROM organizations o
            LEFT JOIN organization_users ou ON ou.organization_id = o.id
            WHERE o.public_id = @Id
            GROUP BY o.id
            """;

        var result = await connection.QueryFirstOrDefaultAsync<AdminOrganizationResponse>(
            sql, new { request.Id });

        if (result is null)
            return AdminOrganizationErrors.NotFound;

        return result;
    }
}
