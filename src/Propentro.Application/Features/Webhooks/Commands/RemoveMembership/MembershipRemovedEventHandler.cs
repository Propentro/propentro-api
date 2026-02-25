using System.Text.Json;
using Dapper;
using Propentro.Application.Common.Email;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Webhooks.Commands.RemoveMembership;

public sealed class MembershipRemovedEventHandler(
    IEmailService emailService,
    IDbConnectionFactory dbConnectionFactory) : IOutboxEventHandler
{
    public string EventType => "membership.removed";

    public async Task HandleAsync(string payload, CancellationToken cancellationToken = default)
    {
        var domainEvent = JsonSerializer.Deserialize<MembershipRemovedEvent>(payload, OutboxJsonOptions.Instance);

        Log.Information("Handling {EventType} for user {UserId} in org {OrganizationId}",
            EventType, domainEvent!.UserId, domainEvent.OrganizationId);

        await using var connection = dbConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var row = await connection.QueryFirstOrDefaultAsync<MembershipEmailRow>(
            """
            SELECT u.email, u.first_name, o.name AS organization_name
            FROM users u
            INNER JOIN organizations o ON o.id = @OrganizationId
            WHERE u.id = @UserId
            """,
            new { domainEvent.UserId, domainEvent.OrganizationId });

        if (row is null || string.IsNullOrWhiteSpace(row.Email))
        {
            Log.Warning(
                "Cannot send membership removed email: user {UserId} or org {OrganizationId} not found",
                domainEvent.UserId, domainEvent.OrganizationId);
            return;
        }

        var template = EmailTemplates.MembershipRemovedEmail(
            row.FirstName ?? "there",
            row.OrganizationName ?? "your organization");

        await emailService.SendAsync(
            new EmailMessage(row.Email, template.Subject, template.HtmlBody, template.TextBody),
            cancellationToken);

        Log.Information("Membership removed email sent to user {UserId} for org {OrganizationId}",
            domainEvent.UserId, domainEvent.OrganizationId);
    }

    private sealed class MembershipEmailRow
    {
        public string Email { get; init; } = null!;
        public string? FirstName { get; init; }
        public string? OrganizationName { get; init; }
    }
}
