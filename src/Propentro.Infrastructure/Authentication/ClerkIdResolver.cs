using Dapper;
using Propentro.Application.Common.Interfaces.Authentication;

namespace Propentro.Infrastructure.Authentication;

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
        var (userId, publicUserId, fullName) = await ResolveUserAsync(connection, externalUserId);
        var (orgId, publicOrgId) = await ResolveOrgAsync(connection, externalOrgId);

        return new ResolvedIdentity(userId, publicUserId, orgId, publicOrgId, fullName);
    }

    private static async Task<(Guid UserId, string PublicUserId, string FullName)> ResolveUserAsync(
        System.Data.Common.DbConnection connection,
        string? externalUserId)
    {
        if (string.IsNullOrWhiteSpace(externalUserId))
            return (Guid.Empty, string.Empty, string.Empty);

        const string sql = """
            SELECT id, public_id, first_name || ' ' || last_name AS full_name
            FROM users
            WHERE external_id = @ExternalId
              AND auth_provider = @AuthProvider
              AND is_active = true
            """;

        var result = await connection.QueryFirstOrDefaultAsync<UserIdResult>(
            sql, new { ExternalId = externalUserId, AuthProvider = "clerk" });

        return result is null
            ? (Guid.Empty, string.Empty, string.Empty)
            : (result.Id, result.PublicId, result.FullName);
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

        var result = await connection.QueryFirstOrDefaultAsync<OrganizationIdResult>(
            sql, new { ExternalId = externalOrgId, AuthProvider = "clerk" });

        return result is null
            ? (Guid.Empty, string.Empty)
            : (result.Id, result.PublicId);
    }

    private sealed record UserIdResult(Guid Id, string PublicId, string FullName = "");
    private sealed record OrganizationIdResult(Guid Id, string PublicId);
}
