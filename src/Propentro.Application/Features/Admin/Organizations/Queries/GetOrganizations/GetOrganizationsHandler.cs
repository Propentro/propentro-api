using System.Text;
using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Organizations;

namespace Propentro.Application.Features.Admin.Organizations.Queries.GetOrganizations;

public sealed class GetOrganizationsHandler(
    IDbConnectionFactory db) : IQueryHandler<GetOrganizationsQuery, PagedResult<AdminOrganizationListResponse>>
{
    public async Task<Result<PagedResult<AdminOrganizationListResponse>>> Handle(
        GetOrganizationsQuery request, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("Limit", request.PageSize);

        var whereBuilder = new StringBuilder("WHERE 1=1");

        if (request.IsActive.HasValue)
        {
            whereBuilder.Append(" AND o.is_active = @IsActive");
            parameters.Add("IsActive", request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            whereBuilder.Append(" AND (o.name ILIKE @Search OR o.slug ILIKE @Search)");
            parameters.Add("Search", $"%{request.Search}%");
        }

        var where = whereBuilder.ToString();

        var countSql = $"""
            SELECT COUNT(*)
            FROM organizations o
            {where}
            """;

        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        if (total == 0)
            return new PagedResult<AdminOrganizationListResponse>([], total, request.Page, request.PageSize);

        var listSql = $"""
            SELECT
                o.public_id         AS OrganizationId,
                o.name              AS Name,
                o.slug              AS Slug,
                o.subscription_plan AS SubscriptionPlan,
                o.is_active         AS IsActive,
                COUNT(ou.user_id)   AS MemberCount,
                o.created_at        AS CreatedAt
            FROM organizations o
            LEFT JOIN organization_users ou ON ou.organization_id = o.id
            {where}
            GROUP BY o.id, o.public_id, o.name, o.slug, o.subscription_plan,
                     o.is_active, o.created_at
            ORDER BY o.created_at DESC
            OFFSET @Offset LIMIT @Limit
            """;

        var items = (await connection.QueryAsync<AdminOrganizationListResponse>(listSql, parameters)).ToList();

        return new PagedResult<AdminOrganizationListResponse>(items, total, request.Page, request.PageSize);
    }
}
