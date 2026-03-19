using System.Text;
using Dapper;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Organizations.Members;

namespace Propentro.Application.Features.Organizations.Members.Queries.GetOrganizationMembers;

public sealed class GetOrganizationMembersHandler(
    IDbConnectionFactory db,
    IUserContext currentUser) : IQueryHandler<GetOrganizationMembersQuery, PagedResult<MemberListItemResponse>>
{
    public async Task<Result<PagedResult<MemberListItemResponse>>> Handle(
        GetOrganizationMembersQuery request,
        CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("OrganizationId", currentUser.OrganizationId);
        parameters.Add("Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("Limit", request.PageSize);

        var whereBuilder = new StringBuilder("""
            WHERE ou.organization_id = @OrganizationId
            """);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            whereBuilder.Append("""

                  AND ou.status = @Status
                """);
            parameters.Add("Status", request.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.RoleId))
        {
            whereBuilder.Append("""

                  AND EXISTS (
                    SELECT 1 FROM organization_user_roles our2
                    JOIN roles r ON r.id = our2.role_id
                    WHERE our2.user_id = ou.user_id
                      AND our2.organization_id = ou.organization_id
                      AND r.public_id = @RoleId
                      AND r.is_active = true
                  )
                """);
            parameters.Add("RoleId", request.RoleId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            whereBuilder.Append("""

                  AND (u.email ILIKE @Search
                    OR u.first_name ILIKE @Search
                    OR u.last_name ILIKE @Search)
                """);
            parameters.Add("Search", $"%{request.Search}%");
        }

        var where = whereBuilder.ToString();

        var countSql = $"""
            SELECT COUNT(*)
            FROM organization_users ou
            JOIN users u ON u.id = ou.user_id
            {where}
            """;

        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        if (total == 0)
            return new PagedResult<MemberListItemResponse>([], total, request.Page, request.PageSize);

        var listSql = $"""
            SELECT
                u.public_id AS user_id,
                u.email,
                u.first_name,
                u.last_name,
                ou.status,
                ou.created_at AS joined_at,
                u.id AS user_internal_id
            FROM organization_users ou
            JOIN users u ON u.id = ou.user_id
            {where}
            ORDER BY ou.created_at DESC
            OFFSET @Offset LIMIT @Limit
            """;

        var members = (await connection.QueryAsync<MemberQueryResult>(listSql, parameters)).ToList();

        if (members.Count == 0)
            return new PagedResult<MemberListItemResponse>([], total, request.Page, request.PageSize);

        var userInternalIds = members.Select(m => m.UserInternalId).ToArray();

        const string rolesSql = """
            SELECT
                u.id AS user_internal_id,
                r.public_id AS role_id,
                r.name,
                r.is_system
            FROM organization_user_roles our
            JOIN roles r ON r.id = our.role_id
            JOIN users u ON u.id = our.user_id
            WHERE our.organization_id = @OrganizationId
              AND our.user_id = ANY(@UserInternalIds)
              AND r.is_active = true
            """;

        var roleRows = (await connection.QueryAsync<MemberRoleQueryResult>(
            rolesSql,
            new { currentUser.OrganizationId, UserInternalIds = userInternalIds }))
            .ToList();

        var rolesLookup = roleRows
            .GroupBy(r => r.UserInternalId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<MemberRoleResponse>)g.Select(r => new MemberRoleResponse
                {
                    Id = r.RoleId,
                    Name = r.Name,
                    IsSystem = r.IsSystem
                }).ToList());

        var items = members.Select(m => new MemberListItemResponse
            {
                UserId = m.UserId,
                Email = m.Email,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Status = m.Status,
                JoinedAt = m.JoinedAt,
                Roles = rolesLookup.GetValueOrDefault(m.UserInternalId, [])
            })
            .ToList();

        return new PagedResult<MemberListItemResponse>(items, total, request.Page, request.PageSize);
    }

    private sealed class MemberQueryResult
    {
        public string UserId { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string Status { get; init; } = null!;
        public DateTimeOffset JoinedAt { get; init; }
        public Guid UserInternalId { get; init; }
    }

    private sealed class MemberRoleQueryResult
    {
        public Guid UserInternalId { get; init; }
        public string RoleId { get; init; } = null!;
        public string Name { get; init; } = null!;
        public bool IsSystem { get; init; }
    }
}
