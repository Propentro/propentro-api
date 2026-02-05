using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// </summary>
public abstract class Entity : IHasPublicId
{
    /// <summary>
    /// Internal database identifier (UUID v7 for time-ordering).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Public-facing identifier for API responses (Base62 encoded).
    /// </summary>
    public string PublicId { get; set; } = default!;

    protected Entity()
    {
        Id = Guid.CreateVersion7();
        PublicId = ToBase62(Id.ToByteArray());
    }

    private static string ToBase62(byte[] bytes)
    {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        var value = new System.Numerics.BigInteger(bytes, isUnsigned: true, isBigEndian: true);
        if (value == 0) return "0";

        var result = new char[22];
        var index = result.Length;

        while (value > 0)
        {
            value = System.Numerics.BigInteger.DivRem(value, 62, out var remainder);
            result[--index] = chars[(int)remainder];
        }

        return new string(result, index, result.Length - index);
    }
}
