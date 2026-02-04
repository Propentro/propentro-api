namespace Propentro.Application.Common.Interfaces.Time;

/// <summary>
/// Abstraction for date/time operations.
/// Allows for testable time-dependent code by enabling time mocking in tests.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Gets the current UTC date (without time component).
    /// </summary>
    DateOnly Today { get; }

    /// <summary>
    /// Gets the current UTC time (without date component).
    /// </summary>
    TimeOnly TimeOfDay { get; }
}
