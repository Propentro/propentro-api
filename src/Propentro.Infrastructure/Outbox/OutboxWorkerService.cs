using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Serilog;

namespace Propentro.Infrastructure.Outbox;

/// <summary>
/// Background service that processes outbox messages using LISTEN/NOTIFY (primary)
/// with a polling fallback every 30 seconds.
/// </summary>
public sealed class OutboxWorkerService(
    IServiceScopeFactory scopeFactory,
    IDbConnectionFactory connectionFactory) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("Outbox worker service started");

        // Run listener and poller concurrently
        var listenerTask = RunListenerAsync(stoppingToken);
        var pollerTask = RunPollerAsync(stoppingToken);

        await Task.WhenAll(listenerTask, pollerTask);

        Log.Information("Outbox worker service stopped");
    }

    private async Task RunListenerAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var connection = connectionFactory.CreateConnection();
                await connection.OpenAsync(ct);

                if (connection is not NpgsqlConnection npgsqlConnection)
                {
                    Log.Warning("LISTEN/NOTIFY requires NpgsqlConnection, falling back to polling only");
                    return;
                }

                npgsqlConnection.Notification += (_, _) =>
                {
                    // Notification received — processing happens in the handler below
                };

                await using var cmd = npgsqlConnection.CreateCommand();
                cmd.CommandText = "LISTEN outbox_channel";
                await cmd.ExecuteNonQueryAsync(ct);

                Log.Information("Outbox worker listening on 'outbox_channel'");

                while (!ct.IsCancellationRequested)
                {
                    // Wait for notification with a timeout so we can check cancellation
                    await npgsqlConnection.WaitAsync(TimeSpan.FromSeconds(5), ct);
                    await ProcessMessagesAsync(ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Outbox LISTEN/NOTIFY connection failed, reconnecting...");
                await WaitBeforeRetryAsync(ct);
            }
        }
    }

    private async Task RunPollerAsync(CancellationToken ct)
    {
        // Short initial delay to let the app start up
        await Task.Delay(TimeSpan.FromSeconds(5), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessMessagesAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Outbox polling cycle failed");
            }

            await Task.Delay(PollingInterval, ct);
        }
    }

    private async Task ProcessMessagesAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
        await processor.ProcessPendingMessagesAsync(ct);
    }

    private static async Task WaitBeforeRetryAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
        catch (OperationCanceledException)
        {
            // Shutting down
        }
    }
}
