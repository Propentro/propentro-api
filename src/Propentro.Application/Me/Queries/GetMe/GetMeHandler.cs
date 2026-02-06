using Dapper;
using Propentro.Application.Common.Interfaces.Identity;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Interfaces.Persistence;
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
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            return MeErrors.NotAuthenticated;
        }

        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                u.public_id AS UserId,
                u.email AS Email,
                o.public_id AS OrganizationId,
                o.name AS OrganizationName,
                o.slug AS OrganizationSlug
            FROM users u
            LEFT JOIN organization_users ou ON ou.user_id = u.id AND ou.organization_id = @OrganizationId
            LEFT JOIN organizations o ON o.id = ou.organization_id
            WHERE u.id = @UserId
              AND u.is_active = true
            """;

        var result = await connection.QueryFirstOrDefaultAsync<MeQueryResult>(
            sql,
            new { currentUser.UserId, currentUser.OrganizationId });

        if (result is null)
        {
            return MeErrors.UserNotFound;
        }

        var permissions = await permissionService.GetUserPermissionsAsync(
            currentUser.UserId,
            currentUser.OrganizationId,
            cancellationToken);

        var dto = new MeDto(
            result.UserId,
            result.Email ?? currentUser.Email ?? string.Empty,
            new MeOrganizationDto(
                result.OrganizationId ?? string.Empty,
                result.OrganizationName ?? string.Empty,
                result.OrganizationSlug ?? string.Empty),
            permissions.Order().ToList());

        return dto;
    }

    private sealed record MeQueryResult(
        string UserId,
        string? Email,
        string? OrganizationId,
        string? OrganizationName,
        string? OrganizationSlug);
}
