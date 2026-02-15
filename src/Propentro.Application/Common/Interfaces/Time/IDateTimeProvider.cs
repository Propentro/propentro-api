namespace Propentro.Application.Common.Interfaces.Time;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today { get; }
    TimeOnly TimeOfDay { get; }
}
