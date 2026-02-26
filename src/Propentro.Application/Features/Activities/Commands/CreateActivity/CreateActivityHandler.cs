using Dapper;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Activity = Propentro.Domain.Entities.Activities.Activity;

namespace Propentro.Application.Features.Activities.Commands.CreateActivity;

public sealed class CreateActivityHandler(
    IAppDbContext dbContext,
    IDbConnectionFactory db,
    IUserContext currentUser) : ICommandHandler<CreateActivityCommand, string>
{
    public async Task<Result<string>> Handle(CreateActivityCommand request, CancellationToken cancellationToken)
    {
        // Resolve entity internal ID via Dapper
        var entityId = await ResolveEntityIdAsync(request.EntityType, request.EntityId, cancellationToken);
        if (entityId is null)
            return ActivityErrors.ContactNotFound;

        var activity = new Activity(request.EntityType, entityId.Value, request.ActivityType)
        {
            Title = request.Title,
            Description = request.Description
        };

        dbContext.Activities.Add(activity);

        return activity.PublicId;
    }

    private async Task<Guid?> ResolveEntityIdAsync(string entityType, string entityId, CancellationToken ct)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(ct);

        var (table, column) = entityType switch
        {
            "contact" => ("contacts", "public_id"),
            _ => throw new ArgumentException($"Unsupported entity type: {entityType}")
        };

        var sql = $"""
            SELECT id FROM {table}
            WHERE {column} = @EntityId
              AND organization_id = @OrganizationId
              AND is_active = true
            """;

        return await connection.QueryFirstOrDefaultAsync<Guid?>(
            sql,
            new { EntityId = entityId, currentUser.OrganizationId });
    }
}
