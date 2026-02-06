namespace Propentro.Domain.Common;

/// <summary>
/// Static clock for domain layer time access.
/// Uses DateTime.UtcNow by default, can be overridden in tests.
/// </summary>
public static class DomainClock
{
    private static Func<DateTime>? _override;

    public static DateTime UtcNow => _override?.Invoke() ?? DateTime.UtcNow;

    /// <summary>
    /// Override the clock for testing. Returns IDisposable that resets on dispose.
    /// Usage: using (DomainClock.Override(() => fixedTime)) { ... }
    /// </summary>
    public static IDisposable Override(Func<DateTime> provider)
    {
        _override = provider;
        return new ClockReset();
    }

    private sealed class ClockReset : IDisposable
    {
        public void Dispose() => _override = null;
    }
}
