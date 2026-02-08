using System.Security.Cryptography;

namespace Propentro.Domain.Common;

/// <summary>
/// Generates prefixed, cryptographically random public identifiers.
/// Format: {prefix}_{12-hex-chars} (e.g., prp_a3f9c12d8e4b)
/// </summary>
public static class PublicIdGenerator
{
    private const int _hexLength = 12;

    public static string Generate(string prefix)
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        var hex = Convert.ToHexString(bytes).ToLowerInvariant()[.._hexLength];
        return $"{prefix}_{hex}";
    }
}
