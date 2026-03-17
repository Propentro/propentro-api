using Dapper;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Me;

namespace Propentro.Application.Features.Me.Queries.GetMe;

public sealed class GetMeHandler(
    IDbConnectionFactory db,
    IUserContext userContext,
    IPermissionService permissionService) : IQueryHandler<GetMeQuery, MeResponse>
{
    public async Task<Result<MeResponse>> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        // Authentication and user/org resolution are guaranteed by the middleware pipeline:
        // Authentication → UserContextMiddleware → ActiveUserAuthorizationHandler
        // No need to check IsAuthenticated or UserId here.

        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string userSql = """
            SELECT
                u.public_id AS user_id,
                u.email AS email,
                u.first_name AS first_name,
                u.last_name AS last_name,
                o.public_id AS organization_id,
                o.name AS organization_name,
                o.slug AS organization_slug
            FROM users u
            LEFT JOIN organization_users ou ON ou.user_id = u.id AND ou.organization_id = @OrganizationId AND ou.status =  'active'
            LEFT JOIN organizations o ON o.id = ou.organization_id AND o.is_active = true
            WHERE u.id = @UserId
              AND u.is_active = true
            """;

        var result = await connection.QueryFirstOrDefaultAsync<MeQueryResult>(
            userSql,
            new { userContext.UserId, userContext.OrganizationId });

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
            new { userContext.UserId, userContext.OrganizationId }))
            .Order().ToList();

        var permissions = await permissionService.GetUserPermissionsAsync(
            userContext.UserId,
            userContext.OrganizationId,
            cancellationToken);

        var response = new MeResponse(
            result.UserId,
            result.Email,
            result.FirstName,
            result.LastName,
            new MeOrganizationResponse(
                result.OrganizationId,
                result.OrganizationName,
                result.OrganizationSlug),
            roles,
            permissions.Order().ToList());

        return response;
    }

    private sealed class MeQueryResult
    {
        public string UserId { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;
        public string OrganizationId { get; init; } = null!;
        public string OrganizationName { get; init; } = null!;
        public string OrganizationSlug { get; init; } = null!;
    }
}
