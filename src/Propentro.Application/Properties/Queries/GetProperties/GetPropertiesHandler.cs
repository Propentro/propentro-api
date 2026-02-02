using System.Data;
using Dapper;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Properties;

namespace Propentro.Application.Properties.Queries.GetProperties;

public sealed class GetPropertiesHandler(
    IDbConnectionFactory db,
    ICurrentUser currentUser) : IQueryHandler<GetPropertiesQuery, PagedResult<PropertyDto>>
{
    public async Task<Result<PagedResult<PropertyDto>>> Handle(
        GetPropertiesQuery request,
        CancellationToken ct)
    {
        using var connection = db.CreateConnection();

        var offset = (request.Page - 1) * request.PageSize;

        const string countSql = """
            SELECT COUNT(*)
            FROM properties p
            WHERE p.organization_id = @OrganizationId
              AND p.is_active = TRUE
              AND (@Search IS NULL OR p.name ILIKE '%' || @Search || '%')
              AND (@PropertyKind IS NULL OR p.property_kind = @PropertyKind)
            """;

        const string dataSql = """
            SELECT
                p.public_id AS PublicId,
                p.name AS Name,
                p.property_kind AS PropertyKind,
                p.year_built AS YearBuilt,
                p.total_area_sqm AS TotalAreaSqm,
                p.total_floors AS TotalFloors,
                p.reference_code AS ReferenceCode,
                p.created_at AS CreatedAt,
                a.line1 AS AddressLine1,
                a.city AS AddressCity,
                a.country_code AS AddressCountry
            FROM properties p
            LEFT JOIN addresses a ON a.id = p.address_id
            WHERE p.organization_id = @OrganizationId
              AND p.is_active = TRUE
              AND (@Search IS NULL OR p.name ILIKE '%' || @Search || '%')
              AND (@PropertyKind IS NULL OR p.property_kind = @PropertyKind)
            ORDER BY p.created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        var parameters = new
        {
            OrganizationId = currentUser.OrganizationId,
            request.Search,
            request.PropertyKind,
            request.PageSize,
            Offset = offset
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await connection.QueryAsync<PropertyDto>(dataSql, parameters);

        return new PagedResult<PropertyDto>(
            items.ToList(),
            totalCount,
            request.Page,
            request.PageSize
        );
    }
}
