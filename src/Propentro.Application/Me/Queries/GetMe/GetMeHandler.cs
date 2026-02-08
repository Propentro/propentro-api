using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Me;

namespace Propentro.Application.Me.Queries.GetMe;

public sealed class GetMeHandler(
    IDbConnectionFactory db,
    ICurrentUser currentUser,
    IPermissionService permissionService) : IQueryHandler<GetMeQuery, MeDto>
{
    public async Task<Result<MeDto>> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        // Authentication and user/org resolution are guaranteed by the middleware pipeline:
        // Authentication → UserContextMiddleware → ActiveUserAuthorizationHandler
        // No need to check IsAuthenticated or UserId here.

        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string userSql = """
            SELECT
                u.public_id AS UserId,
                u.email AS Email,
                u.first_name AS FirstName,
                u.last_name AS LastName,
                o.public_id AS OrganizationId,
                o.name AS OrganizationName,
                o.slug AS OrganizationSlug
            FROM users u
            LEFT JOIN organization_users ou ON ou.user_id = u.id AND ou.organization_id = @OrganizationId AND ou.status =  'active'
            LEFT JOIN organizations o ON o.id = ou.organization_id AND o.is_active = true
            WHERE u.id = @UserId
              AND u.is_active = true
            """;

        var result = await connection.QueryFirstOrDefaultAsync<MeQueryResult>(
            userSql,
            new { currentUser.UserId, currentUser.OrganizationId });

        if (result is null)
        {
            return MeErrors.UserNotFound;
        }

        const string rolesSql = """
            SELECT DISTINCT r.name
            FROM organization_user_roles our
            JOIN roles r ON r.id = our.role_id
            WHERE our.user_id = @UserId
              AND our.organization_id = @OrganizationId
              AND r.is_active = true
            """;

        var roles = (await connection.QueryAsync<string>(
            rolesSql,
            new { currentUser.UserId, currentUser.OrganizationId }))
            .Order().ToList();

        var permissions = await permissionService.GetUserPermissionsAsync(
            currentUser.UserId,
            currentUser.OrganizationId,
            cancellationToken);

        var dto = new MeDto(
            result.UserId,
            result.Email,
            result.FirstName,
            result.LastName,
            new MeOrganizationDto(
                result.OrganizationId,
                result.OrganizationName,
                result.OrganizationSlug),
            roles,
            permissions.Order().ToList());

        return dto;
    }

    private sealed record MeQueryResult(
        string UserId,
        string Email,
        string FirstName,
        string LastName,
        string OrganizationId,
        string OrganizationName,
        string OrganizationSlug);
}
