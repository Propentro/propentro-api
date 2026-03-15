using System.Text;
using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Invites;

namespace Propentro.Application.Features.Admin.Invites.Queries.GetInvites;

public sealed class GetInvitesHandler(
    IDbConnectionFactory db) : IQueryHandler<GetInvitesQuery, PagedResult<InviteResponse>>
{
    public async Task<Result<PagedResult<InviteResponse>>> Handle(
        GetInvitesQuery request, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("Limit", request.PageSize);

        var whereBuilder = new StringBuilder("WHERE 1=1");

        var status = request.Status ?? "pending";
        whereBuilder.Append(" AND i.status = @Status");
        parameters.Add("Status", status);

        var where = whereBuilder.ToString();

        var countSql = $"""
            SELECT COUNT(*)
            FROM invites i
            {where}
            """;

        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        if (total == 0)
            return new PagedResult<InviteResponse>([], total, request.Page, request.PageSize);

        var listSql = $"""
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
            {where}
            ORDER BY i.created_at DESC
            OFFSET @Offset LIMIT @Limit
            """;

        var items = (await connection.QueryAsync<InviteResponse>(listSql, parameters)).ToList();

        return new PagedResult<InviteResponse>(items, total, request.Page, request.PageSize);
    }
}
