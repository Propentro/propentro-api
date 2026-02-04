using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Units;

namespace Propentro.Application.Units.Queries.GetUnitById;

public sealed class GetUnitByIdHandler(
    IDbConnectionFactory db,
    ICurrentUser currentUser) : IQueryHandler<GetUnitByIdQuery, UnitDetailDto>
{
    public async Task<Result<UnitDetailDto>> Handle(
        GetUnitByIdQuery request,
        CancellationToken ct)
    {
        using var connection = db.CreateConnection();

        const string sql = """
            SELECT
                u.public_id AS PublicId,
                u.name AS Name,
                u.unit_type AS UnitType,
                u.usage_type AS UsageType,
                u.status AS Status,
                u.rental_mode AS RentalMode,
                u.floor AS Floor,
                u.unit_number AS UnitNumber,
                u.building_section AS BuildingSection,
                u.bedrooms AS Bedrooms,
                u.bathrooms AS Bathrooms,
                u.area_sqm AS AreaSqm,
                u.furnished_status AS FurnishedStatus,
                u.default_rent_minor AS DefaultRentMinor,
                u.default_deposit_minor AS DefaultDepositMinor,
                u.default_currency AS DefaultCurrency,
                u.max_occupants AS MaxOccupants,
                u.note AS Note,
                u.created_at AS CreatedAt,
                u.updated_at AS UpdatedAt,
                p.public_id AS PropertyPublicId,
                p.name AS PropertyName
            FROM units u
            LEFT JOIN properties p ON p.id = u.property_id
            WHERE u.public_id = @PublicId
              AND u.organization_id = @OrganizationId
              AND u.is_active = TRUE
            """;

        var unit = await connection.QueryFirstOrDefaultAsync<UnitDetailDto>(
            sql,
            new { request.PublicId, currentUser.OrganizationId });

        if (unit is null)
        {
            return UnitErrors.NotFoundById(request.PublicId);
        }

        return unit;
    }
}
