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
    private const int MaxEntries = 10_000;
    private static readonly TimeSpan EventTtl = TimeSpan.FromDays(7);

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
        var cutoff = DateTimeOffset.UtcNow - EventTtl;
        var keysToRemove = _processedEvents
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
            _processedEvents.TryRemove(key, out _);
    }
}
