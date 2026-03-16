using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Users;

namespace Propentro.Application.Features.Admin.Users.Queries.GetOrganizationMembers;

public sealed class GetAdminOrganizationMembersHandler(
    IDbConnectionFactory db) : IQueryHandler<GetAdminOrganizationMembersQuery, List<AdminMemberResponse>>
{
    public async Task<Result<List<AdminMemberResponse>>> Handle(
        GetAdminOrganizationMembersQuery request, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
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
            """;

        var members = (await connection.QueryAsync<AdminMemberResponse>(
            sql, new { request.OrganizationId })).ToList();

        return members;
    }
}
