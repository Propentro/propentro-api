using System.Text;
using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Users;

namespace Propentro.Application.Features.Admin.Users.Queries.GetUsers;

public sealed class GetUsersHandler(
    IDbConnectionFactory db) : IQueryHandler<GetUsersQuery, PagedResult<AdminUserListResponse>>
{
    public async Task<Result<PagedResult<AdminUserListResponse>>> Handle(
        GetUsersQuery request, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("Limit", request.PageSize);

        var whereBuilder = new StringBuilder("WHERE 1=1");

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            whereBuilder.Append(" AND u.is_active = @IsActive");
            parameters.Add("IsActive", request.Status.Equals("active", StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            whereBuilder.Append(" AND (u.email ILIKE @Search OR u.first_name ILIKE @Search OR u.last_name ILIKE @Search)");
            parameters.Add("Search", $"%{request.Search}%");
        }

        var where = whereBuilder.ToString();

        var countSql = $"""
            SELECT COUNT(*)
            FROM users u
            {where}
            """;

        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        if (total == 0)
            return new PagedResult<AdminUserListResponse>([], total, request.Page, request.PageSize);

        var listSql = $"""
            SELECT
                u.public_id                     AS user_id,
                u.email                         AS email,
                u.first_name                    AS first_name,
                u.last_name                     AS last_name,
                u.is_active                     AS is_active,
                COUNT(DISTINCT ou.organization_id) AS organization_count,
                u.created_at                    AS created_at
            FROM users u
            LEFT JOIN organization_users ou ON ou.user_id = u.id
            {where}
            GROUP BY u.id, u.public_id, u.email, u.first_name, u.last_name,
                     u.is_active, u.created_at
            ORDER BY u.created_at DESC
            OFFSET @Offset LIMIT @Limit
            """;

        var items = (await connection.QueryAsync<AdminUserListResponse>(listSql, parameters)).ToList();

        return new PagedResult<AdminUserListResponse>(items, total, request.Page, request.PageSize);
    }
}
