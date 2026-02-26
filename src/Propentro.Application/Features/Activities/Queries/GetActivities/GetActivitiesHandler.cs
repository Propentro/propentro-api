using Dapper;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Activities;

namespace Propentro.Application.Features.Activities.Queries.GetActivities;

public sealed class GetActivitiesHandler(
    IDbConnectionFactory db,
    IUserContext currentUser) : IQueryHandler<GetActivitiesQuery, PagedResult<ActivityResponse>>
{
    public async Task<Result<PagedResult<ActivityResponse>>> Handle(
        GetActivitiesQuery request,
        CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        // Resolve entity internal ID
        var entityId = await ResolveEntityIdAsync(connection, request.EntityType, request.EntityId);
        if (entityId is null)
            return ActivityErrors.ContactNotFound;

        const string countSql = """
            SELECT COUNT(*)
            FROM activities a
            WHERE a.entity_type = @EntityType
              AND a.entity_id = @EntityId
              AND a.organization_id = @OrganizationId
              AND a.is_active = true
            """;

        var total = await connection.ExecuteScalarAsync<int>(
            countSql,
            new { request.EntityType, EntityId = entityId.Value, currentUser.OrganizationId });

        const string listSql = """
            SELECT
                a.public_id AS id,
                a.activity_type,
                a.title,
                a.description,
                a.is_pinned,
                a.metadata,
                u.first_name || ' ' || u.last_name AS created_by,
                a.created_at,
                a.updated_at
            FROM activities a
            LEFT JOIN users u ON u.id = a.created_by_id
            WHERE a.entity_type = @EntityType
              AND a.entity_id = @EntityId
              AND a.organization_id = @OrganizationId
              AND a.is_active = true
            ORDER BY a.is_pinned DESC, a.created_at DESC
            OFFSET @Offset LIMIT @Limit
            """;

        var activities = (await connection.QueryAsync<ActivityQueryResult>(
            listSql,
            new
            {
                request.EntityType,
                EntityId = entityId.Value,
                currentUser.OrganizationId,
                Offset = (request.Page - 1) * request.PageSize,
                Limit = request.PageSize
            }))
            .Select(a => new ActivityResponse(
                a.Id,
                a.ActivityType,
                a.Title,
                a.Description,
                a.IsPinned,
                null,
                a.CreatedBy,
                a.CreatedAt,
                a.UpdatedAt))
            .ToList();

        return new PagedResult<ActivityResponse>(activities, total, request.Page, request.PageSize);
    }

    private static async Task<Guid?> ResolveEntityIdAsync(
        System.Data.Common.DbConnection connection,
        string entityType,
        string entityId)
    {
        var table = entityType switch
        {
            "contact" => "contacts",
            _ => throw new ArgumentException($"Unsupported entity type: {entityType}")
        };

        var sql = $"SELECT id FROM {table} WHERE public_id = @EntityId AND is_active = true";
        return await connection.QueryFirstOrDefaultAsync<Guid?>(sql, new { EntityId = entityId });
    }

    private sealed class ActivityQueryResult
    {
        public string Id { get; init; } = null!;
        public string ActivityType { get; init; } = null!;
        public string? Title { get; init; }
        public string? Description { get; init; }
        public bool IsPinned { get; init; }
        public string? CreatedBy { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
