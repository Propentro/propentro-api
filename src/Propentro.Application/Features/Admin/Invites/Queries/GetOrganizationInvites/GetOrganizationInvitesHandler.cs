using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Invites;

namespace Propentro.Application.Features.Admin.Invites.Queries.GetOrganizationInvites;

public sealed class GetOrganizationInvitesHandler(
    IDbConnectionFactory db) : IQueryHandler<GetOrganizationInvitesQuery, List<InviteResponse>>
{
    public async Task<Result<List<InviteResponse>>> Handle(
        GetOrganizationInvitesQuery request, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                i.public_id                              AS InviteId,
                o.public_id                              AS OrganizationId,
                o.name                                   AS OrganizationName,
                i.email                                  AS Email,
                r.name                                   AS RoleName,
                i.status                                 AS Status,
                COALESCE(u.first_name || ' ' || u.last_name, '') AS InvitedBy,
                i.expires_at                             AS ExpiresAt,
                i.created_at                             AS CreatedAt
            FROM invites i
            JOIN organizations o ON o.id = i.organization_id
            JOIN roles r ON r.id = i.role_id
            LEFT JOIN users u ON u.id = i.invited_by_id
            WHERE i.organization_id = (SELECT id FROM organizations WHERE public_id = @OrgPublicId)
            ORDER BY i.created_at DESC
            """;

        var items = (await connection.QueryAsync<InviteResponse>(sql, new { request.OrgPublicId })).ToList();

        return items;
    }
}
