using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Interfaces.Time;

namespace Propentro.Infrastructure.Services;

/// <summary>
/// Provides the current UTC date and time.
/// This implementation uses system time; tests can use a mock implementation.
/// </summary>
public sealed class DateTimeProvider : IDateTimeProvider, Application.Common.Interfaces.IDateTimeProvider
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;

    /// <inheritdoc />
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <inheritdoc />
    public TimeOnly TimeOfDay => TimeOnly.FromDateTime(DateTime.UtcNow);
}
