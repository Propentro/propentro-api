using System.Text.Json;
using Dapper;
using Propentro.Application.Common.Email;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Webhooks.Commands.SyncUser;

public sealed class UserCreatedEventHandler(
    IEmailService emailService,
    IDbConnectionFactory dbConnectionFactory) : IOutboxEventHandler
{
    public string EventType => "user.created";

    public async Task HandleAsync(string payload, CancellationToken cancellationToken = default)
    {
        var domainEvent = JsonSerializer.Deserialize<UserCreatedEvent>(payload);

        Log.Information("Handling {EventType} for user {UserId}", EventType, domainEvent!.UserId);

        await using var connection = dbConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var user = await connection.QueryFirstOrDefaultAsync<UserEmailRow>(
            "SELECT email, first_name FROM users WHERE id = @Id",
            new { Id = domainEvent.UserId });

        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            Log.Warning("Cannot send welcome email: user {UserId} not found or has no email", domainEvent.UserId);
            return;
        }

        var template = EmailTemplates.WelcomeEmail(user.FirstName ?? "there");

        await emailService.SendAsync(
            new EmailMessage(user.Email, template.Subject, template.HtmlBody, template.TextBody),
            cancellationToken);

        Log.Information("Welcome email sent to user {UserId}", domainEvent.UserId);
    }

    private sealed class UserEmailRow
    {
        public string Email { get; init; } = null!;
        public string? FirstName { get; init; }
    }
}
