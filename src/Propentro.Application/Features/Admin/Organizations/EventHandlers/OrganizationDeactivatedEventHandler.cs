using Dapper;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Messaging;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Admin.Organizations.EventHandlers;

public sealed class OrganizationDeactivatedEventHandler(
    IDbConnectionFactory db,
    IEmailService emailService,
    IEmailTemplateService templateService) : OutboxEventHandler<OrganizationDeactivatedEvent>
{
    public override string EventType => "organization.deactivated";

    protected override async Task HandleAsync(OrganizationDeactivatedEvent @event, CancellationToken cancellationToken)
    {
        if (@event.MemberUserIds.Count == 0)
            return;

        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var orgName = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT name FROM organizations WHERE id = @OrgId",
            new { OrgId = @event.OrganizationId });

        if (orgName is null)
            return;

        // Query users directly by IDs from event payload — organization_users rows
        // are hard-deleted by DeactivateOrganizationHandler, so we can't join through them.
        // MemberUserIds were captured before deletion and embedded in the event.
        var members = await connection.QueryAsync<MemberInfo>(
            """
            SELECT u.email, u.first_name AS name
            FROM users u
            WHERE u.id IN @UserIds
            """,
            new { UserIds = @event.MemberUserIds.Select(id => id.ToString()).ToList() });

        foreach (var member in members)
        {
            var email = templateService.OrganizationDeactivated(member.Email, member.Name ?? "there", orgName);
            var result = await emailService.SendAsync(email, cancellationToken);

            if (result.IsFailure)
                throw new InvalidOperationException($"Failed to send organization.deactivated email to {member.Email}: {result.Error.Code}");
        }
    }

    private sealed record MemberInfo
    {
        public required string Email { get; init; }
        public string? Name { get; init; }
    }
}
