using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Users;

namespace Propentro.Application.Features.Admin.Users.Queries.GetUser;

public sealed class GetUserHandler(
    IDbConnectionFactory db) : IQueryHandler<GetUserQuery, AdminUserResponse>
{
    public async Task<Result<AdminUserResponse>> Handle(
        GetUserQuery request, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string userSql = """
            SELECT
                u.public_id  AS user_id,
                u.email      AS email,
                u.first_name AS first_name,
                u.last_name  AS last_name,
                u.is_active  AS is_active,
                u.created_at AS created_at
            FROM users u
            WHERE u.public_id = @Id
            """;

        var user = await connection.QueryFirstOrDefaultAsync<AdminUserResponse>(
            userSql, new { request.Id });

        if (user is null)
            return AdminUserErrors.UserNotFound;

        const string orgsSql = """
            SELECT
                o.public_id    AS organization_id,
                o.name         AS name,
                ou.status      AS status,
                ou.created_at  AS joined_at
            FROM organization_users ou
            JOIN organizations o ON o.id = ou.organization_id
            WHERE ou.user_id = (SELECT id FROM users WHERE public_id = @Id)
            """;

        var orgs = (await connection.QueryAsync<UserOrganizationInfo>(
            orgsSql, new { request.Id })).ToList();

        const string rolesSql = """
            SELECT
                o.public_id AS organization_id,
                r.public_id AS id,
                r.name      AS name
            FROM organization_user_roles our
            JOIN organizations o ON o.id = our.organization_id
            JOIN roles r ON r.id = our.role_id
            WHERE our.user_id = (SELECT id FROM users WHERE public_id = @Id)
            """;

        var roles = (await connection.QueryAsync<OrgRoleRow>(
            rolesSql, new { request.Id })).ToList();

        var rolesByOrg = roles.GroupBy(r => r.OrganizationId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<MemberRoleInfo>)g
                .Select(r => new MemberRoleInfo { Id = r.Id, Name = r.Name })
                .ToList());

        var organizations = orgs.Select(o => o with
        {
            Roles = rolesByOrg.GetValueOrDefault(o.OrganizationId, [])
        }).ToList();

        return user with { Organizations = organizations };
    }

    private sealed record OrgRoleRow
    {
        public string OrganizationId { get; init; } = null!;
        public string Id { get; init; } = null!;
        public string Name { get; init; } = null!;
    }
}
