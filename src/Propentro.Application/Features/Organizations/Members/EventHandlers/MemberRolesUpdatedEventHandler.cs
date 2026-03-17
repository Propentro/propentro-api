using Dapper;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Messaging;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Organizations.Members.EventHandlers;

public sealed class MemberRolesUpdatedEventHandler(
    IDbConnectionFactory db,
    IEmailService emailService,
    IEmailTemplateService templateService) : OutboxEventHandler<MemberRolesUpdatedEvent>
{
    public override string EventType => "member.roles_updated";

    protected override async Task HandleAsync(MemberRolesUpdatedEvent @event, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var recipient = await connection.QuerySingleOrDefaultAsync<RecipientInfo>(
            """
            SELECT u.email, u.first_name AS name
            FROM users u
            WHERE u.id = @UserId
            """,
            new { UserId = @event.AggregateId });

        if (recipient is null)
            return;

        var orgName = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT name FROM organizations WHERE id = @OrgId",
            new { OrgId = @event.OrganizationId });

        if (orgName is null)
            return;

        var roleNames = (await connection.QueryAsync<string>(
            """
            SELECT r.name
            FROM organization_user_roles our
            JOIN roles r ON r.id = our.role_id
            WHERE our.user_id = @UserId AND our.organization_id = @OrgId
            """,
            new { UserId = @event.AggregateId, OrgId = @event.OrganizationId })).ToList();

        if (roleNames.Count == 0)
            return;

        var email = templateService.MemberRolesUpdated(recipient.Email, recipient.Name ?? "there", orgName, roleNames);
        var result = await emailService.SendAsync(email, cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to send member.roles_updated email to {recipient.Email}: {result.Error.Code}");
    }

    private sealed record RecipientInfo
    {
        public required string Email { get; init; }
        public string? Name { get; init; }
    }
}
