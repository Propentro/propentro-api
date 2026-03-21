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

        const string membersSql = """
            SELECT
                u.public_id   AS user_id,
                u.email       AS email,
                u.first_name  AS first_name,
                u.last_name   AS last_name,
                ou.status     AS status,
                ou.created_at AS joined_at
            FROM organization_users ou
            JOIN users u ON u.id = ou.user_id
            WHERE ou.organization_id = (SELECT id FROM organizations WHERE public_id = @OrganizationId)
            ORDER BY ou.created_at DESC
            OFFSET @Offset LIMIT @Limit
            """;

        var members = (await connection.QueryAsync<AdminMemberResponse>(membersSql, parameters)).ToList();

        var userPublicIds = members.Select(m => m.UserId).ToList();

        const string rolesSql = """
            SELECT
                u.public_id AS user_id,
                r.public_id AS id,
                r.name      AS name
            FROM organization_user_roles our
            JOIN users u ON u.id = our.user_id
            JOIN roles r ON r.id = our.role_id
            WHERE our.organization_id = (SELECT id FROM organizations WHERE public_id = @OrganizationId)
              AND u.public_id = ANY(@UserPublicIds)
            """;

        var roles = (await connection.QueryAsync<MemberRoleRow>(
            rolesSql, new { request.OrganizationId, UserPublicIds = userPublicIds })).ToList();

        var rolesByUser = roles.GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<MemberRoleInfo>)g
                .Select(r => new MemberRoleInfo { Id = r.Id, Name = r.Name })
                .ToList());

        var result = members.Select(m => m with
        {
            Roles = rolesByUser.GetValueOrDefault(m.UserId, [])
        }).ToList();

        return new PagedResult<AdminMemberResponse>(result, total, request.Page, request.PageSize);
    }

    private sealed record MemberRoleRow
    {
        public string UserId { get; init; } = null!;
        public string Id { get; init; } = null!;
        public string Name { get; init; } = null!;
    }
}
