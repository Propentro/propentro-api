using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Users;

namespace Propentro.Application.Features.Admin.Users.Queries.GetOrganizationMembers;

public sealed class GetAdminOrganizationMembersHandler(
    IDbConnectionFactory db) : IQueryHandler<GetAdminOrganizationMembersQuery, PagedResult<AdminMemberResponse>>
{
    public async Task<Result<PagedResult<AdminMemberResponse>>> Handle(
        GetAdminOrganizationMembersQuery request, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("OrganizationId", request.OrganizationId);
        parameters.Add("Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("Limit", request.PageSize);

        const string countSql = """
            SELECT COUNT(*)
            FROM organization_users ou
            WHERE ou.organization_id = (SELECT id FROM organizations WHERE public_id = @OrganizationId)
            """;

        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        if (total == 0)
            return new PagedResult<AdminMemberResponse>([], total, request.Page, request.PageSize);

        const string listSql = """
            SELECT
                u.public_id  AS user_id,
                u.email      AS email,
                u.first_name AS first_name,
                u.last_name  AS last_name,
                r.name       AS role_name,
                r.public_id  AS role_public_id,
                ou.created_at AS joined_at
            FROM organization_users ou
            JOIN users u ON u.id = ou.user_id
            LEFT JOIN organization_user_roles our ON our.organization_id = ou.organization_id
                                                 AND our.user_id = ou.user_id
            LEFT JOIN roles r ON r.id = our.role_id
            WHERE ou.organization_id = (SELECT id FROM organizations WHERE public_id = @OrganizationId)
            ORDER BY ou.created_at DESC
            OFFSET @Offset LIMIT @Limit
            """;

        var members = (await connection.QueryAsync<AdminMemberResponse>(
            listSql, parameters)).ToList();

        return new PagedResult<AdminMemberResponse>(members, total, request.Page, request.PageSize);
    }
}
