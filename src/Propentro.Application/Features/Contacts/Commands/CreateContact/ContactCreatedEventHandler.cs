using System.Text.Json;
using Dapper;
using Propentro.Application.Common.Interfaces.Notifications;
using Propentro.Domain.Entities.Contacts.Events;

namespace Propentro.Application.Features.Contacts.Commands.CreateContact;

public sealed class ContactCreatedEventHandler(
    IDbConnectionFactory db,
    INotificationService notificationService) : IOutboxEventHandler
{
    public string EventType => "contact.created";

    public async Task HandleAsync(string payload, CancellationToken cancellationToken = default)
    {
        var domainEvent = JsonSerializer.Deserialize<ContactCreatedEvent>(payload, OutboxJsonOptions.Instance);

        Log.Information("Handling {EventType} for contact {ContactId}", EventType, domainEvent!.ContactId);

        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var contactName = await connection.QueryFirstOrDefaultAsync<string>(
            "SELECT display_name FROM contacts WHERE id = @Id",
            new { Id = domainEvent.ContactId });

        var displayName = contactName ?? "Unknown";

        await notificationService.CreateForOrgAdminsAsync(
            domainEvent.OrganizationId,
            $"New contact added",
            $"New contact \"{displayName}\" has been added.",
            "system",
            "info",
            "contact",
            domainEvent.ContactId.ToString(),
            senderId: null,
            cancellationToken);
    }
}
