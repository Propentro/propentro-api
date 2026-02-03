using Dapper;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Leases;

namespace Propentro.Application.Leases.Queries.GetLeaseById;

public sealed class GetLeaseByIdHandler(
    IDbConnectionFactory db,
    ICurrentUser currentUser) : IQueryHandler<GetLeaseByIdQuery, LeaseDetailDto>
{
    public async Task<Result<LeaseDetailDto>> Handle(
        GetLeaseByIdQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = db.CreateConnection();

        const string sql = """
            SELECT
                l.public_id AS PublicId,
                l.lease_type AS LeaseType,
                l.status AS Status,
                l.start_date AS StartDate,
                l.end_date AS EndDate,
                l.move_in_date AS MoveInDate,
                l.move_out_date AS MoveOutDate,
                l.rent_minor AS RentMinor,
                l.currency AS Currency,
                l.rent_frequency AS RentFrequency,
                l.rent_due_day AS RentDueDay,
                l.deposit_minor AS DepositMinor,
                l.deposit_status AS DepositStatus,
                l.notice_period_days AS NoticePeriodDays,
                l.reference_code AS ReferenceCode,
                l.note AS Note,
                l.created_at AS CreatedAt,
                l.updated_at AS UpdatedAt
            FROM leases l
            WHERE l.public_id = @PublicId
              AND l.organization_id = @OrganizationId
              AND l.is_active = TRUE
            """;

        var lease = await connection.QueryFirstOrDefaultAsync<LeaseDetailDto>(
            sql,
            new { request.PublicId, currentUser.OrganizationId });

        if (lease is null)
        {
            return LeaseErrors.NotFoundById(request.PublicId);
        }

        return lease;
    }
}
