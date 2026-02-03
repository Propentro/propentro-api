using Dapper;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Leases;

namespace Propentro.Application.Leases.Queries.GetLeases;

public sealed class GetLeasesHandler(
    IDbConnectionFactory db,
    ICurrentUser currentUser) : IQueryHandler<GetLeasesQuery, PagedResult<LeaseDto>>
{
    public async Task<Result<PagedResult<LeaseDto>>> Handle(
        GetLeasesQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = db.CreateConnection();

        var offset = (request.Page - 1) * request.PageSize;

        const string countSql = """
            SELECT COUNT(DISTINCT l.id)
            FROM leases l
            LEFT JOIN lease_units lu ON lu.lease_id = l.id
            WHERE l.organization_id = @OrganizationId
              AND l.is_active = TRUE
              AND (@Status IS NULL OR l.status = @Status)
              AND (@UnitId IS NULL OR lu.unit_id = @UnitId)
            """;

        const string dataSql = """
            SELECT
                l.public_id AS PublicId,
                l.lease_type AS LeaseType,
                l.status AS Status,
                l.start_date AS StartDate,
                l.end_date AS EndDate,
                l.rent_minor AS RentMinor,
                l.currency AS Currency,
                l.rent_frequency AS RentFrequency,
                l.reference_code AS ReferenceCode,
                l.created_at AS CreatedAt
            FROM leases l
            LEFT JOIN lease_units lu ON lu.lease_id = l.id
            WHERE l.organization_id = @OrganizationId
              AND l.is_active = TRUE
              AND (@Status IS NULL OR l.status = @Status)
              AND (@UnitId IS NULL OR lu.unit_id = @UnitId)
            GROUP BY l.id
            ORDER BY l.created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        var parameters = new
        {
            OrganizationId = currentUser.OrganizationId,
            request.Status,
            request.UnitId,
            request.PageSize,
            Offset = offset
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await connection.QueryAsync<LeaseDto>(dataSql, parameters);

        return new PagedResult<LeaseDto>(
            items.ToList(),
            totalCount,
            request.Page,
            request.PageSize
        );
    }
}
