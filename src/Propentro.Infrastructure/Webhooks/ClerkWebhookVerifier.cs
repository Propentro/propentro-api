using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Propentro.Infrastructure.Webhooks;

/// <summary>
/// Verifies Clerk webhook signatures using the Svix standard (HMAC-SHA256).
/// Clerk uses Svix for webhook delivery. Signatures are in the svix-signature header.
/// </summary>
public sealed class ClerkWebhookVerifier
{
    private readonly byte[] _secretBytes;
    private static readonly TimeSpan TimestampTolerance = TimeSpan.FromMinutes(5);

    public ClerkWebhookVerifier(IConfiguration configuration)
    {
        var secret = configuration["Clerk:WebhookSecret"]
            ?? throw new InvalidOperationException("Clerk:WebhookSecret not configured");

        // Svix secrets are prefixed with "whsec_" and base64-encoded
        if (secret.StartsWith("whsec_", StringComparison.Ordinal))
            secret = secret["whsec_".Length..];

        _secretBytes = Convert.FromBase64String(secret);
    }

    /// <summary>
    /// Verifies the Svix webhook signature.
    /// </summary>
    /// <param name="svixId">The svix-id header value.</param>
    /// <param name="svixTimestamp">The svix-timestamp header value.</param>
    /// <param name="svixSignature">The svix-signature header value (comma-separated list).</param>
    /// <param name="body">The raw request body.</param>
    /// <returns>True if the signature is valid.</returns>
    public bool Verify(string? svixId, string? svixTimestamp, string? svixSignature, string body)
    {
        if (string.IsNullOrEmpty(svixId) ||
            string.IsNullOrEmpty(svixTimestamp) ||
            string.IsNullOrEmpty(svixSignature))
        {
            Log.Warning("Webhook verification failed: missing Svix headers");
            return false;
        }

        // Validate timestamp to prevent replay attacks
        if (!long.TryParse(svixTimestamp, out var timestampSeconds))
        {
            Log.Warning("Webhook verification failed: invalid timestamp format");
            return false;
        }

        var timestamp = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
        var age = DateTimeOffset.UtcNow - timestamp;

        if (age.Duration() > TimestampTolerance)
        {
            Log.Warning("Webhook verification failed: timestamp {Timestamp} is outside tolerance ({Age}s)",
                timestamp, age.TotalSeconds);
            return false;
        }

        // Compute expected signature
        var signedContent = $"{svixId}.{svixTimestamp}.{body}";
        var expectedSignature = ComputeSignature(signedContent);

        // svix-signature header contains comma-separated versioned signatures: "v1,base64sig"
        var signatures = svixSignature.Split(' ');
        foreach (var sig in signatures)
        {
            var parts = sig.Split(',', 2);
            if (parts.Length == 2 && parts[0] == "v1")
            {
                if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expectedSignature),
                    Encoding.UTF8.GetBytes(parts[1])))
                {
                    return true;
                }
            }
        }

        Log.Warning("Webhook verification failed: no matching signature found");
        return false;
    }

    private string ComputeSignature(string content)
    {
        using var hmac = new HMACSHA256(_secretBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hash);
    }
}
