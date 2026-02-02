using Propentro.Domain.Common;

namespace Propentro.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public long MinorUnits { get; }
    public string CurrencyCode { get; }

    private Money(long minorUnits, string currencyCode)
    {
        MinorUnits = minorUnits;
        CurrencyCode = currencyCode;
    }

    public static Money Create(long minorUnits, string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        return new Money(minorUnits, currencyCode.ToUpperInvariant());
    }

    public static Money Zero(string currencyCode) => Create(0, currencyCode);

    public Money Add(Money other)
    {
        if (CurrencyCode != other.CurrencyCode)
        {
            throw new InvalidOperationException("Cannot add money with different currencies");
        }

        return Create(MinorUnits + other.MinorUnits, CurrencyCode);
    }

    public Money Subtract(Money other)
    {
        if (CurrencyCode != other.CurrencyCode)
        {
            throw new InvalidOperationException("Cannot subtract money with different currencies");
        }

        return Create(MinorUnits - other.MinorUnits, CurrencyCode);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MinorUnits;
        yield return CurrencyCode;
    }

    public override string ToString() => $"{MinorUnits} {CurrencyCode}";
}
