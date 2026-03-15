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
                u.public_id  AS UserId,
                u.email      AS Email,
                u.first_name AS FirstName,
                u.last_name  AS LastName,
                u.is_active  AS IsActive,
                u.created_at AS CreatedAt
            FROM users u
            WHERE u.public_id = @PublicId
            """;

        var user = await connection.QueryFirstOrDefaultAsync<AdminUserResponse>(
            userSql, new { request.PublicId });

        if (user is null)
            return AdminUserErrors.UserNotFound;

        const string orgsSql = """
            SELECT
                o.public_id AS OrganizationId,
                o.name      AS Name,
                r.name      AS RoleName
            FROM organization_users ou
            JOIN organizations o ON o.id = ou.organization_id
            JOIN organization_user_roles our ON our.organization_id = ou.organization_id
                                            AND our.user_id = ou.user_id
            JOIN roles r ON r.id = our.role_id
            WHERE ou.user_id = (SELECT id FROM users WHERE public_id = @PublicId)
            """;

        var organizations = (await connection.QueryAsync<UserOrganizationInfo>(
            orgsSql, new { request.PublicId })).ToList();

        return user with { Organizations = organizations };
    }
}
