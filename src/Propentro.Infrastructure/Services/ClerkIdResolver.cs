using Dapper;

namespace Propentro.Infrastructure.Services;

/// <summary>
/// Resolves Clerk authentication provider IDs to internal database IDs.
/// Pure ID resolution only — no caching, no membership checks, no business rules.
/// If Clerk is replaced with another auth provider, only this class needs to change.
/// </summary>
public sealed class ClerkIdResolver(IDbConnectionFactory db) : IExternalIdResolver
{
    public async Task<ResolvedIdentity> ResolveIdentityAsync(
        string? externalUserId,
        string? externalOrgId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        // Npgsql does not support concurrent commands on a single connection
        var (userId, publicUserId) = await ResolveUserAsync(connection, externalUserId);
        var (orgId, publicOrgId) = await ResolveOrgAsync(connection, externalOrgId);

        return new ResolvedIdentity(userId, publicUserId, orgId, publicOrgId);
    }

    private static async Task<(Guid UserId, string PublicUserId)> ResolveUserAsync(
        System.Data.Common.DbConnection connection,
        string? externalUserId)
    {
        if (string.IsNullOrWhiteSpace(externalUserId))
            return (Guid.Empty, string.Empty);

        const string sql = """
            SELECT id, public_id
            FROM users
            WHERE external_id = @ExternalId
              AND auth_provider = @AuthProvider
              AND is_active = true
            """;

        var result = await connection.QueryFirstOrDefaultAsync<IdResult>(
            sql, new { ExternalId = externalUserId, AuthProvider = "clerk" });

        return result is null
            ? (Guid.Empty, string.Empty)
            : (result.Id, result.PublicId);
    }

    private static async Task<(Guid OrgId, string PublicOrgId)> ResolveOrgAsync(
        System.Data.Common.DbConnection connection,
        string? externalOrgId)
    {
        if (string.IsNullOrWhiteSpace(externalOrgId))
            return (Guid.Empty, string.Empty);

        const string sql = """
            SELECT id, public_id
            FROM organizations
            WHERE external_id = @ExternalId
              AND auth_provider = @AuthProvider
              AND is_active = true
            """;

        var result = await connection.QueryFirstOrDefaultAsync<IdResult>(
            sql, new { ExternalId = externalOrgId, AuthProvider = "clerk" });

        return result is null
            ? (Guid.Empty, string.Empty)
            : (result.Id, result.PublicId);
    }

    private sealed record IdResult(Guid Id, string PublicId);
}
