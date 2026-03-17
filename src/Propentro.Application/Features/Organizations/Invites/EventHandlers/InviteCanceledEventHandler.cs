using Dapper;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Messaging;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Organizations.Invites.EventHandlers;

public sealed class InviteCanceledEventHandler(
    IDbConnectionFactory db,
    IEmailService emailService,
    IEmailTemplateService templateService) : OutboxEventHandler<InviteCanceledEvent>
{
    public override string EventType => "invite.canceled";

    protected override async Task HandleAsync(InviteCanceledEvent @event, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var inviteEmail = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT email FROM invites WHERE id = @InviteId",
            new { InviteId = @event.AggregateId });

        if (inviteEmail is null)
            return;

        var orgName = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT name FROM organizations WHERE id = @OrgId",
            new { OrgId = @event.OrganizationId });

        if (orgName is null)
            return;

        var email = templateService.InviteCanceled(inviteEmail, orgName);
        var result = await emailService.SendAsync(email, cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to send invite.canceled email to {inviteEmail}: {result.Error.Code}");
    }
}
