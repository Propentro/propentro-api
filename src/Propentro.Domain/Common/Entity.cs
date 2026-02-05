using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Automatically generates Id and PublicId on construction.
/// </summary>
public abstract class Entity : IHasPublicId
{
    /// <summary>
    /// Internal database identifier (UUID v7 for time-ordering).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Public-facing identifier for API responses.
    /// Uses a shorter, URL-safe format derived from the Id.
    /// </summary>
    public string PublicId { get; set; } = default!;

    protected Entity()
    {
        // Generate UUID v7 for time-ordered IDs (better for database indexing)
        Id = GenerateTimeOrderedGuid();
        // Generate a shorter, URL-safe public ID
        PublicId = GeneratePublicId();
    }

    /// <summary>
    /// Generates a time-ordered GUID (similar to UUID v7).
    /// The first 48 bits are based on Unix timestamp for natural ordering.
    /// </summary>
    private static Guid GenerateTimeOrderedGuid()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var randomBytes = new byte[10];
        Random.Shared.NextBytes(randomBytes);

        var guidBytes = new byte[16];

        // First 6 bytes: Unix timestamp in milliseconds (big-endian)
        guidBytes[0] = (byte)(timestamp >> 40);
        guidBytes[1] = (byte)(timestamp >> 32);
        guidBytes[2] = (byte)(timestamp >> 24);
        guidBytes[3] = (byte)(timestamp >> 16);
        guidBytes[4] = (byte)(timestamp >> 8);
        guidBytes[5] = (byte)timestamp;

        // Remaining 10 bytes: random
        Array.Copy(randomBytes, 0, guidBytes, 6, 10);

        // Set version (7) and variant bits
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x70); // Version 7
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80); // Variant

        return new Guid(guidBytes);
    }

    /// <summary>
    /// Generates a URL-safe public identifier.
    /// Format: Base62 encoding of UUID, resulting in ~22 characters.
    /// </summary>
    private string GeneratePublicId()
    {
        // Use base62 encoding for a shorter, URL-safe ID
        var bytes = Id.ToByteArray();
        return ToBase62(bytes);
    }

    private static string ToBase62(byte[] bytes)
    {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        // Convert bytes to a big integer representation
        var value = new System.Numerics.BigInteger(bytes, isUnsigned: true, isBigEndian: true);
        if (value == 0) return "0";

        var result = new char[22]; // Max length for 128-bit number in base62
        var index = result.Length;

        while (value > 0)
        {
            value = System.Numerics.BigInteger.DivRem(value, 62, out var remainder);
            result[--index] = chars[(int)remainder];
        }

        return new string(result, index, result.Length - index);
    }
}
