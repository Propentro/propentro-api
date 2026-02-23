using System.Collections.Concurrent;

namespace Propentro.Infrastructure.Webhooks;

/// <summary>
/// In-memory store for tracking processed webhook event IDs to ensure idempotency.
/// Uses a bounded concurrent dictionary with TTL-based eviction to prevent unbounded memory growth.
/// </summary>
/// <remarks>
/// TODO: For multi-instance deployment, move idempotency to Redis or database.
/// </remarks>
public sealed class WebhookIdempotencyStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _processedEvents = new();
    private const int _maxEntries = 10_000;
    private static readonly TimeSpan _eventTtl = TimeSpan.FromDays(7);

    /// <summary>
    /// Tries to mark an event ID as processed. Returns false if already processed.
    /// Atomic — uses TryAdd directly to avoid TOCTOU race condition.
    /// </summary>
    public bool TryMarkProcessed(string eventId)
    {
        // Evict old entries if at capacity
        if (_processedEvents.Count >= _maxEntries)
            EvictOldEntries();

        return _processedEvents.TryAdd(eventId, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Removes a previously processed event ID so it can be retried.
    /// Used when handler processing fails after idempotency was recorded.
    /// </summary>
    public bool TryRemoveProcessed(string eventId)
    {
        return _processedEvents.TryRemove(eventId, out _);
    }

    private void EvictOldEntries()
    {
        var cutoff = DateTimeOffset.UtcNow - _eventTtl;
        var keysToRemove = _processedEvents
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
            _processedEvents.TryRemove(key, out _);
    }
}
