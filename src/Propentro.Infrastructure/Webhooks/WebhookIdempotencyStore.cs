using System.Collections.Concurrent;

namespace Propentro.Infrastructure.Webhooks;

/// <summary>
/// In-memory store for tracking processed webhook event IDs to ensure idempotency.
/// Uses a bounded concurrent dictionary to prevent unbounded memory growth.
/// </summary>
public sealed class WebhookIdempotencyStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _processedEvents = new();
    private const int MaxEntries = 10_000;

    /// <summary>
    /// Tries to mark an event ID as processed. Returns false if already processed.
    /// </summary>
    public bool TryMarkProcessed(string eventId)
    {
        if (_processedEvents.ContainsKey(eventId))
            return false;

        // Evict old entries if at capacity
        if (_processedEvents.Count >= MaxEntries)
            EvictOldEntries();

        return _processedEvents.TryAdd(eventId, DateTimeOffset.UtcNow);
    }

    private void EvictOldEntries()
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-1);
        var keysToRemove = _processedEvents
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
            _processedEvents.TryRemove(key, out _);
    }
}
