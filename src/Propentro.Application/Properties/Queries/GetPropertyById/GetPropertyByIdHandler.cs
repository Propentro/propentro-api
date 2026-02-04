using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Properties;

namespace Propentro.Application.Properties.Queries.GetPropertyById;

public sealed class GetPropertyByIdHandler(
    IDbConnectionFactory db,
    ICurrentUser currentUser) : IQueryHandler<GetPropertyByIdQuery, PropertyDetailDto>
{
    public async Task<Result<PropertyDetailDto>> Handle(
        GetPropertyByIdQuery request,
        CancellationToken ct)
    {
        using var connection = db.CreateConnection();

        const string sql = """
            SELECT
                p.public_id AS PublicId,
                p.name AS Name,
                p.property_kind AS PropertyKind,
                p.year_built AS YearBuilt,
                p.total_area_sqm AS TotalAreaSqm,
                p.total_floors AS TotalFloors,
                p.reference_code AS ReferenceCode,
                p.note AS Note,
                p.created_at AS CreatedAt,
                p.updated_at AS UpdatedAt,
                a.public_id AS AddressPublicId,
                a.line1 AS AddressLine1,
                a.line2 AS AddressLine2,
                a.city AS AddressCity,
                a.region AS AddressRegion,
                a.postal_code AS AddressPostalCode,
                a.country_code AS AddressCountry,
                parent.public_id AS ParentPropertyPublicId,
                parent.name AS ParentPropertyName
            FROM properties p
            LEFT JOIN addresses a ON a.id = p.address_id
            LEFT JOIN properties parent ON parent.id = p.parent_property_id
            WHERE p.public_id = @PublicId
              AND p.organization_id = @OrganizationId
              AND p.is_active = TRUE
            """;

        var property = await connection.QueryFirstOrDefaultAsync<PropertyDetailDto>(
            sql,
            new { request.PublicId, currentUser.OrganizationId });

        if (property is null)
        {
            return PropertyErrors.NotFoundById(request.PublicId);
        }

        return property;
    }
}
