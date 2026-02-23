namespace Propentro.Infrastructure.Clock;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
    public TimeOnly TimeOfDay => TimeOnly.FromDateTime(DateTime.UtcNow);
}
