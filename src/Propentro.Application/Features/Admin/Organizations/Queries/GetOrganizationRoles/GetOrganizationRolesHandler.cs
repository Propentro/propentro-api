using Dapper;
using Propentro.Application.Common.Errors;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Organizations;

namespace Propentro.Application.Features.Admin.Organizations.Queries.GetOrganizationRoles;

public sealed class GetOrganizationRolesHandler(
    IDbConnectionFactory db) : IQueryHandler<GetOrganizationRolesQuery, List<AdminOrgRoleResponse>>
{
    public async Task<Result<List<AdminOrgRoleResponse>>> Handle(
        GetOrganizationRolesQuery request, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        // Resolve internal org ID from public ID
        const string orgSql = "SELECT id FROM organizations WHERE public_id = @PublicId";
        var orgId = await connection.QueryFirstOrDefaultAsync<Guid?>(
            orgSql, new { PublicId = request.OrganizationPublicId });

        if (orgId is null)
            return OrganizationErrors.NotFound;

        const string sql = """
            SELECT
                r.public_id  AS role_id,
                r.name       AS name,
                r.is_system  AS is_system
            FROM roles r
            WHERE (r.organization_id = @OrgId OR (r.is_system = TRUE AND r.organization_id IS NULL))
              AND r.is_active = TRUE
            ORDER BY r.is_system DESC, r.name ASC
            """;

        var roles = await connection.QueryAsync<AdminOrgRoleResponse>(
            sql, new { OrgId = orgId });

        return roles.ToList();
    }
}
