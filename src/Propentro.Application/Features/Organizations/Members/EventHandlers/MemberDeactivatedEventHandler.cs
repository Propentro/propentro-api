using Dapper;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Messaging;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Organizations.Members.EventHandlers;

public sealed class MemberDeactivatedEventHandler(
    IDbConnectionFactory db,
    IEmailService emailService,
    IEmailTemplateService templateService) : OutboxEventHandler<MemberDeactivatedEvent>
{
    public override string EventType => "member.deactivated";

    protected override async Task HandleAsync(MemberDeactivatedEvent @event, CancellationToken cancellationToken)
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

        var email = templateService.MemberDeactivated(recipient.Email, recipient.Name ?? "there", orgName);
        var result = await emailService.SendAsync(email, cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to send member.deactivated email to {recipient.Email}: {result.Error.Code}");
    }

    private sealed record RecipientInfo
    {
        public required string Email { get; init; }
        public string? Name { get; init; }
    }
}
