using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Invites;

namespace Propentro.Application.Features.Organizations.Invites.Queries.GetCurrentOrgInvites;

public sealed class GetCurrentOrgInvitesHandler(
    IDbConnectionFactory db) : IQueryHandler<GetCurrentOrgInvitesQuery, PagedResult<InviteResponse>>
{
    public async Task<Result<PagedResult<InviteResponse>>> Handle(
        GetCurrentOrgInvitesQuery request, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("OrganizationId", request.OrganizationId);
        parameters.Add("Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("Limit", request.PageSize);

        const string countSql = """
            SELECT COUNT(*)
            FROM invites i
            WHERE i.organization_id = @OrganizationId
            """;

        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        if (total == 0)
            return new PagedResult<InviteResponse>([], total, request.Page, request.PageSize);

        const string listSql = """
            SELECT
                i.public_id                              AS invite_id,
                o.public_id                              AS organization_id,
                o.name                                   AS organization_name,
                i.email                                  AS email,
                r.name                                   AS role_name,
                i.status                                 AS status,
                COALESCE(u.first_name || ' ' || u.last_name, '') AS invited_by,
                i.expires_at                             AS expires_at,
                i.created_at                             AS created_at
            FROM invites i
            JOIN organizations o ON o.id = i.organization_id
            JOIN roles r ON r.id = i.role_id
            LEFT JOIN users u ON u.id = i.invited_by_id
            WHERE i.organization_id = @OrganizationId
            ORDER BY i.created_at DESC
            OFFSET @Offset LIMIT @Limit
            """;

        var items = (await connection.QueryAsync<InviteResponse>(listSql, parameters)).ToList();

        return new PagedResult<InviteResponse>(items, total, request.Page, request.PageSize);
    }
}
