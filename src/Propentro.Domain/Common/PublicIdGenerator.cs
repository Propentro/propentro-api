using System.Security.Cryptography;

namespace Propentro.Domain.Common;

/// <summary>
/// Generates cryptographically random public identifiers for entities.
/// These IDs are URL-safe, lowercase alphanumeric strings.
/// </summary>
public static class PublicIdGenerator
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
    private const int DefaultLength = 20;

    /// <summary>
    /// Generates a random public identifier.
    /// Default 20 chars = 36^20 (~10^31) possible values.
    /// </summary>
    public static string Generate(int length = DefaultLength)
    {
        return string.Create(length, 0, static (span, _) =>
        {
            Span<byte> bytes = stackalloc byte[span.Length];
            RandomNumberGenerator.Fill(bytes);
            for (var i = 0; i < span.Length; i++)
                span[i] = Alphabet[bytes[i] % Alphabet.Length];
        });
    }
}
