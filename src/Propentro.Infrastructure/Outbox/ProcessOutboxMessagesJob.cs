using Dapper;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace Propentro.Infrastructure.Outbox;

[DisallowConcurrentExecution]
internal sealed class ProcessOutboxMessagesJob(
    IDbConnectionFactory dbConnectionFactory,
    IDateTimeProvider dateTimeProvider,
    IOptions<OutboxOptions> outboxOptions) : IJob
{
    private readonly int _batchSize = outboxOptions.Value.BatchSize;

    public async Task Execute(IJobExecutionContext context)
    {
        Log.Debug("Beginning to process outbox messages");

        await using var connection = dbConnectionFactory.CreateConnection();
        await connection.OpenAsync(context.CancellationToken);

        var messages = await connection.QueryAsync<OutboxMessageRow>(
            """
            SELECT id, event_type, retry_count, max_retries
            FROM outbox_messages
            WHERE status = 'pending'
              AND (next_retry_at IS NULL OR next_retry_at <= now())
            ORDER BY created_at
            LIMIT @BatchSize
            FOR UPDATE SKIP LOCKED
            """,
            new { BatchSize = _batchSize });

        var messageList = messages.ToList();

        if (messageList.Count == 0)
        {
            Log.Debug("No outbox messages to process");
            return;
        }

        Log.Information("Processing {MessageCount} outbox messages", messageList.Count);

        foreach (var message in messageList)
        {
            await ProcessMessageAsync(message, context.CancellationToken);
        }

        Log.Debug("Completed processing outbox messages");
    }

    private async Task ProcessMessageAsync(OutboxMessageRow message, CancellationToken cancellationToken)
    {
        await using var connection = dbConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // TODO: Add handler dispatch here when first async side-effect is needed
            // For now, just mark as completed.

            var now = dateTimeProvider.UtcNow;

            await connection.ExecuteAsync(
                """
                UPDATE outbox_messages
                SET status = 'completed',
                    processed_at = @ProcessedAt
                WHERE id = @Id
                """,
                new { Id = message.Id, ProcessedAt = now },
                transaction);

            await transaction.CommitAsync(cancellationToken);

            Log.Debug("Processed outbox message {MessageId} ({EventType})", message.Id, message.EventType);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            Log.Error(ex, "Failed to process outbox message {MessageId} ({EventType})", message.Id, message.EventType);

            await ApplyRetryOrFailAsync(message, ex);
        }
    }

    private async Task ApplyRetryOrFailAsync(OutboxMessageRow message, Exception exception)
    {
        await using var connection = dbConnectionFactory.CreateConnection();
        await connection.OpenAsync();

        var newRetryCount = message.RetryCount + 1;
        var now = dateTimeProvider.UtcNow;

        if (newRetryCount >= message.MaxRetries)
        {
            await connection.ExecuteAsync(
                """
                UPDATE outbox_messages
                SET status = 'failed',
                    retry_count = @RetryCount,
                    processed_at = @ProcessedAt,
                    last_error = @LastError
                WHERE id = @Id
                """,
                new
                {
                    Id = message.Id,
                    RetryCount = newRetryCount,
                    ProcessedAt = now,
                    LastError = exception.Message
                });

            Log.Warning(
                "Outbox message {MessageId} permanently failed after {RetryCount} retries",
                message.Id,
                newRetryCount);
        }
        else
        {
            var backoffSeconds = Math.Pow(2, newRetryCount);
            var nextRetryAt = now.AddSeconds(backoffSeconds);

            await connection.ExecuteAsync(
                """
                UPDATE outbox_messages
                SET retry_count = @RetryCount,
                    next_retry_at = @NextRetryAt,
                    last_error = @LastError
                WHERE id = @Id
                """,
                new
                {
                    Id = message.Id,
                    RetryCount = newRetryCount,
                    NextRetryAt = nextRetryAt,
                    LastError = exception.Message
                });

            Log.Debug(
                "Outbox message {MessageId} scheduled for retry {RetryCount}/{MaxRetries} at {NextRetryAt}",
                message.Id,
                newRetryCount,
                message.MaxRetries,
                nextRetryAt);
        }
    }

    private sealed class OutboxMessageRow
    {
        public Guid Id { get; init; }
        public string EventType { get; init; } = null!;
        public int RetryCount { get; init; }
        public int MaxRetries { get; init; }
    }
}
