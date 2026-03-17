using Dapper;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Messaging;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Organizations.Members.EventHandlers;

public sealed class MemberReactivatedEventHandler(
    IDbConnectionFactory db,
    IEmailService emailService,
    IEmailTemplateService templateService) : OutboxEventHandler<MemberReactivatedEvent>
{
    public override string EventType => "member.reactivated";

    protected override async Task HandleAsync(MemberReactivatedEvent @event, CancellationToken cancellationToken)
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

        var email = templateService.MemberReactivated(recipient.Email, recipient.Name ?? "there", orgName);
        var result = await emailService.SendAsync(email, cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to send member.reactivated email to {recipient.Email}: {result.Error.Code}");
    }

    private sealed record RecipientInfo
    {
        public required string Email { get; init; }
        public string? Name { get; init; }
    }
}
