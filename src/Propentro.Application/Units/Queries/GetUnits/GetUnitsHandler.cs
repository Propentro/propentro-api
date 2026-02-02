using Dapper;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Units;

namespace Propentro.Application.Units.Queries.GetUnits;

public sealed class GetUnitsHandler(
    IDbConnectionFactory db,
    ICurrentUser currentUser) : IQueryHandler<GetUnitsQuery, PagedResult<UnitDto>>
{
    public async Task<Result<PagedResult<UnitDto>>> Handle(
        GetUnitsQuery request,
        CancellationToken ct)
    {
        using var connection = db.CreateConnection();

        var offset = (request.Page - 1) * request.PageSize;

        const string countSql = """
            SELECT COUNT(*)
            FROM units u
            WHERE u.organization_id = @OrganizationId
              AND u.is_active = TRUE
              AND (@Search IS NULL OR u.name ILIKE '%' || @Search || '%')
              AND (@PropertyId IS NULL OR u.property_id = @PropertyId)
              AND (@Status IS NULL OR u.status = @Status)
            """;

        const string dataSql = """
            SELECT
                u.public_id AS PublicId,
                u.name AS Name,
                u.unit_type AS UnitType,
                u.usage_type AS UsageType,
                u.status AS Status,
                u.floor AS Floor,
                u.unit_number AS UnitNumber,
                u.bedrooms AS Bedrooms,
                u.bathrooms AS Bathrooms,
                u.area_sqm AS AreaSqm,
                u.default_rent_minor AS DefaultRentMinor,
                u.default_currency AS DefaultCurrency,
                u.created_at AS CreatedAt,
                p.public_id AS PropertyPublicId,
                p.name AS PropertyName
            FROM units u
            LEFT JOIN properties p ON p.id = u.property_id
            WHERE u.organization_id = @OrganizationId
              AND u.is_active = TRUE
              AND (@Search IS NULL OR u.name ILIKE '%' || @Search || '%')
              AND (@PropertyId IS NULL OR u.property_id = @PropertyId)
              AND (@Status IS NULL OR u.status = @Status)
            ORDER BY u.created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        var parameters = new
        {
            OrganizationId = currentUser.OrganizationId,
            request.Search,
            request.PropertyId,
            request.Status,
            request.PageSize,
            Offset = offset
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await connection.QueryAsync<UnitDto>(dataSql, parameters);

        return new PagedResult<UnitDto>(
            items.ToList(),
            totalCount,
            request.Page,
            request.PageSize
        );
    }
}
