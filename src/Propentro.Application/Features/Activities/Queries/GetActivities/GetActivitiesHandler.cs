using System.Text.Json;
using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Activities;

namespace Propentro.Application.Features.Activities.Queries.GetActivities;

public sealed class GetActivitiesHandler(
    IDbConnectionFactory db,
    IUserContext userContext) : IQueryHandler<GetActivitiesQuery, PagedResult<ActivityResponse>>
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
            new { request.ContactId, userContext.OrganizationId });

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
            new { EntityId = contactId.Value, userContext.OrganizationId });

        var offset = (request.Page - 1) * request.PageSize;

        const string dataSql = """
            SELECT
                a.public_id AS Id,
                a.activity_type AS ActivityType,
                a.title AS Title,
                a.description AS Description,
                a.is_pinned AS IsPinned,
                a.metadata AS Metadata,
                CONCAT(u.first_name, ' ', u.last_name) AS CreatedBy,
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

        var rows = (await connection.QueryAsync<ActivityQueryResult>(
            dataSql,
            new { EntityId = contactId.Value, userContext.OrganizationId, Offset = offset, PageSize = request.PageSize }))
            .ToList();

        var items = rows.Select(r => new ActivityResponse(
            r.Id,
            r.ActivityType,
            r.Title,
            r.Description,
            r.IsPinned,
            DeserializeMetadata(r.Metadata),
            r.CreatedBy,
            r.CreatedAt,
            r.UpdatedAt)).ToList();

        return new PagedResult<ActivityResponse>(items, totalCount, request.Page, request.PageSize);
    }

    private static Dictionary<string, object>? DeserializeMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata) || metadata == "{}")
            return null;

        return JsonSerializer.Deserialize<Dictionary<string, object>>(metadata);
    }

    private sealed class ActivityQueryResult
    {
        public string Id { get; init; } = null!;
        public string ActivityType { get; init; } = null!;
        public string? Title { get; init; }
        public string? Description { get; init; }
        public bool IsPinned { get; init; }
        public string? Metadata { get; init; }
        public string? CreatedBy { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
