using System.Data.Common;
using Dapper;
using Propentro.Domain.Entities;
using Serilog;

namespace Propentro.Infrastructure.Outbox;

/// <summary>
/// Processes pending outbox messages by fetching them with SELECT FOR UPDATE SKIP LOCKED,
/// resolving the appropriate handler, and updating message status.
/// </summary>
public sealed class OutboxProcessor(
    IDbConnectionFactory connectionFactory,
    IEnumerable<IOutboxEventHandler> handlers,
    IDateTimeProvider dateTimeProvider)
{
    private readonly Dictionary<string, IOutboxEventHandler> _handlers =
        handlers.ToDictionary(h => h.EventType, h => h, StringComparer.OrdinalIgnoreCase);

    public async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        var messages = await FetchPendingMessagesAsync(connection, ct);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(connection, message, ct);
        }
    }

    private static async Task<IEnumerable<OutboxMessage>> FetchPendingMessagesAsync(
        DbConnection connection,
        CancellationToken ct)
    {
        const string sql = """
            SELECT id, organization_id, event_type, aggregate_type, aggregate_id,
                   payload, status, retry_count, max_retries, next_retry_at,
                   last_error, created_at, processed_at, created_by_id, request_id
            FROM outbox_messages
            WHERE status IN ('pending', 'failed')
              AND (next_retry_at IS NULL OR next_retry_at <= now())
            ORDER BY created_at
            LIMIT 50
            FOR UPDATE SKIP LOCKED
            """;

        return await connection.QueryAsync<OutboxMessage>(
            new CommandDefinition(sql, cancellationToken: ct));
    }

    private async Task ProcessMessageAsync(
        DbConnection connection,
        OutboxMessage message,
        CancellationToken ct)
    {
        try
        {
            await SetStatusAsync(connection, message.Id, "processing", ct);

            if (_handlers.TryGetValue(message.EventType, out var handler))
            {
                await handler.HandleAsync(message, ct);

                Log.Information(
                    "Outbox message {MessageId} processed successfully (type: {EventType})",
                    message.Id, message.EventType);
            }
            else
            {
                Log.Debug(
                    "No handler registered for outbox event type {EventType}, marking as completed",
                    message.EventType);
            }

            await MarkCompletedAsync(connection, message.Id, ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Failed to process outbox message {MessageId} (type: {EventType}, attempt: {RetryCount})",
                message.Id, message.EventType, message.RetryCount + 1);

            await MarkFailedAsync(connection, message, ex, ct);
        }
    }

    private static async Task SetStatusAsync(
        DbConnection connection, Guid messageId, string status, CancellationToken ct)
    {
        const string sql = "UPDATE outbox_messages SET status = @Status WHERE id = @Id";
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = messageId, Status = status }, cancellationToken: ct));
    }

    private async Task MarkCompletedAsync(
        DbConnection connection, Guid messageId, CancellationToken ct)
    {
        const string sql = """
            UPDATE outbox_messages
            SET status = 'completed', processed_at = @Now
            WHERE id = @Id
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = messageId, Now = dateTimeProvider.UtcNow }, cancellationToken: ct));
    }

    private async Task MarkFailedAsync(
        DbConnection connection, OutboxMessage message, Exception ex, CancellationToken ct)
    {
        var newRetryCount = message.RetryCount + 1;
        var newStatus = newRetryCount >= message.MaxRetries ? "dead_letter" : "failed";
        var nextRetryMinutes = Math.Pow(2, newRetryCount);
        var nextRetryAt = newStatus == "dead_letter"
            ? (DateTimeOffset?)null
            : dateTimeProvider.UtcNow.AddMinutes(nextRetryMinutes);

        const string sql = """
            UPDATE outbox_messages
            SET status = @Status,
                retry_count = @RetryCount,
                next_retry_at = @NextRetryAt,
                last_error = @LastError
            WHERE id = @Id
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = message.Id,
            Status = newStatus,
            RetryCount = newRetryCount,
            NextRetryAt = nextRetryAt,
            LastError = ex.ToString()[..Math.Min(ex.ToString().Length, 4000)]
        }, cancellationToken: ct));

        if (newStatus == "dead_letter")
        {
            Log.Warning(
                "Outbox message {MessageId} moved to dead letter after {MaxRetries} retries (type: {EventType})",
                message.Id, message.MaxRetries, message.EventType);
        }
    }
}
