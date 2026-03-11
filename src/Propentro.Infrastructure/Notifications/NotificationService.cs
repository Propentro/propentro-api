using System.Text.Json;
using Dapper;
using Propentro.Application.Common.Interfaces.Notifications;
using Propentro.Domain.Common;
using Serilog;

namespace Propentro.Infrastructure.Notifications;

internal sealed class NotificationService(IDbConnectionFactory db) : INotificationService
{
    public async Task CreateForOrgAdminsAsync(
        Guid organizationId,
        string title,
        string body,
        string category,
        string severity,
        string? entityType,
        string? entityId,
        Guid? senderId,
        CancellationToken ct)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(ct);

        const string adminsSql = """
            SELECT DISTINCT ou.user_id
            FROM organization_users ou
            INNER JOIN organization_user_roles our ON our.user_id = ou.user_id
                AND our.organization_id = ou.organization_id
            INNER JOIN roles r ON r.id = our.role_id
            WHERE ou.organization_id = @OrganizationId
              AND ou.is_active = true
              AND r.name = 'admin'
              AND r.is_active = true
            """;

        var adminUserIds = (await connection.QueryAsync<Guid>(adminsSql, new { OrganizationId = organizationId }))
            .ToList();

        if (adminUserIds.Count == 0)
        {
            Log.Warning("No admin users found for organization {OrganizationId}, skipping notification", organizationId);
            return;
        }

        var data = new Dictionary<string, string?>();
        if (entityType is not null) data["entityType"] = entityType;
        if (entityId is not null) data["entityId"] = entityId;

        var dataJson = data.Count > 0
            ? JsonSerializer.Serialize(data)
            : "{}";

        const string insertSql = """
            INSERT INTO notifications (id, public_id, organization_id, user_id, title, body, category, severity, data, sender_id, created_at)
            VALUES (@Id, @PublicId, @OrganizationId, @UserId, @Title, @Body, @Category, @Severity, @Data::jsonb, @SenderId, CURRENT_TIMESTAMP)
            """;

        foreach (var userId in adminUserIds)
        {
            var id = Guid.CreateVersion7();
            var publicId = PublicIdGenerator.Generate(Notification.PublicIdPrefix);

            await connection.ExecuteAsync(insertSql, new
            {
                Id = id,
                PublicId = publicId,
                OrganizationId = organizationId,
                UserId = userId,
                Title = title,
                Body = body,
                Category = category,
                Severity = severity,
                Data = dataJson,
                SenderId = senderId
            });
        }

        Log.Information(
            "Created {Count} notifications for org {OrganizationId}: {Title}",
            adminUserIds.Count, organizationId, title);
    }
}
