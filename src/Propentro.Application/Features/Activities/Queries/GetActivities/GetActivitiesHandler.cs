using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Activities;

namespace Propentro.Application.Features.Activities.Queries.GetActivities;

public sealed class GetActivitiesHandler(
    IDbConnectionFactory db,
    ICurrentUser currentUser) : IQueryHandler<GetActivitiesQuery, PagedResult<ActivityResponse>>
{
    public async Task<Result<PagedResult<ActivityResponse>>> Handle(
        GetActivitiesQuery request,
        CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string contactCheckSql = """
            SELECT id FROM contacts
            WHERE public_id = @ContactId
              AND organization_id = @OrganizationId
              AND is_active = true
            """;

        var contactId = await connection.QueryFirstOrDefaultAsync<Guid?>(
            contactCheckSql,
            new { request.ContactId, currentUser.OrganizationId });

        if (contactId is null)
            return ActivityErrors.ContactNotFound;

        const string countSql = """
            SELECT COUNT(1)
            FROM activities a
            WHERE a.entity_type = 'contact'
              AND a.entity_id = @EntityId
              AND a.organization_id = @OrganizationId
              AND a.is_active = true
            """;

        var totalCount = await connection.ExecuteScalarAsync<int>(
            countSql,
            new { EntityId = contactId.Value, currentUser.OrganizationId });

        var offset = (request.Page - 1) * request.PageSize;

        const string dataSql = """
            SELECT
                a.public_id AS ActivityId,
                a.activity_type AS ActivityType,
                a.title AS Title,
                a.description AS Description,
                a.is_pinned AS IsPinned,
                CONCAT(u.first_name, ' ', u.last_name) AS CreatedByName,
                a.created_at AS CreatedAt,
                a.updated_at AS UpdatedAt
            FROM activities a
            LEFT JOIN users u ON u.id = a.created_by_id AND u.is_active = true
            WHERE a.entity_type = 'contact'
              AND a.entity_id = @EntityId
              AND a.organization_id = @OrganizationId
              AND a.is_active = true
            ORDER BY a.is_pinned DESC, a.created_at DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var items = (await connection.QueryAsync<ActivityResponse>(
            dataSql,
            new { EntityId = contactId.Value, currentUser.OrganizationId, Offset = offset, PageSize = request.PageSize }))
            .ToList();

        return new PagedResult<ActivityResponse>(items, totalCount, request.Page, request.PageSize);
    }
}
