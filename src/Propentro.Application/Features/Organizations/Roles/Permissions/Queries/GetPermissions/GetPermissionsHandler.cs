using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Organizations.Roles.Permissions;

namespace Propentro.Application.Features.Organizations.Roles.Permissions.Queries.GetPermissions;

public sealed class GetPermissionsHandler(
    IDbConnectionFactory db) : IQueryHandler<GetPermissionsQuery, IReadOnlyList<PermissionGroupResponse>>
{
    public async Task<Result<IReadOnlyList<PermissionGroupResponse>>> Handle(
        GetPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT id, code, group_name, description
            FROM permissions
            ORDER BY group_name, code
            """;

        var rows = (await connection.QueryAsync<PermissionQueryResult>(sql)).ToList();

        var result = rows
            .GroupBy(p => p.GroupName)
            .OrderBy(g => g.Key)
            .Select(g => new PermissionGroupResponse
            {
                Group = g.Key,
                Permissions = g.Select(p => new PermissionItemResponse
                {
                    Code = p.Code,
                    Description = p.Description
                }).ToList()
            })
            .ToList();

        return result;
    }

    private sealed class PermissionQueryResult
    {
        public string Code { get; init; } = null!;
        public string GroupName { get; init; } = null!;
        public string? Description { get; init; }
    }
}
