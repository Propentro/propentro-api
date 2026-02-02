using Propentro.Domain.Common;

namespace Propentro.Domain.ValueObjects;

public sealed class DateRange : ValueObject
{
    public DateOnly Start { get; }
    public DateOnly? End { get; }

    private DateRange(DateOnly start, DateOnly? end)
    {
        Start = start;
        End = end;
    }

    public static DateRange Create(DateOnly start, DateOnly? end = null)
    {
        if (end.HasValue && end.Value < start)
        {
            throw new ArgumentException("End date must be after start date");
        }

        return new DateRange(start, end);
    }

    public bool Contains(DateOnly date)
    {
        if (date < Start)
        {
            return false;
        }

        return !End.HasValue || date <= End.Value;
    }

    public bool Overlaps(DateRange other)
    {
        // If either range has no end, check only start dates
        if (!End.HasValue && !other.End.HasValue)
        {
            return true;
        }

        if (!End.HasValue)
        {
            return other.End >= Start;
        }

        if (!other.End.HasValue)
        {
            return End >= other.Start;
        }

        return Start <= other.End.Value && other.Start <= End.Value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }

    public override string ToString() => End.HasValue
        ? $"{Start:yyyy-MM-dd} - {End.Value:yyyy-MM-dd}"
        : $"{Start:yyyy-MM-dd} - ongoing";
}
