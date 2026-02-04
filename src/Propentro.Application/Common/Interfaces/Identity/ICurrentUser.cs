namespace Propentro.Application.Common.Interfaces.Identity;

/// <summary>
/// Provides access to the current authenticated user's context.
/// This service is scoped per-request and extracts user information from JWT claims.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Internal Organization ID from the database (Guid).
    /// Returns Guid.Empty if not authenticated or organization not resolved.
    /// </summary>
    Guid OrganizationId { get; }

    /// <summary>
    /// Internal User ID from the database (Guid).
    /// Returns Guid.Empty if not authenticated or user not resolved.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// User's public identifier (user-facing ID string).
    /// Returns empty string if not authenticated or user not resolved.
    /// </summary>
    string PublicUserId { get; }

    /// <summary>
    /// External User ID from the auth provider (e.g., "user_xxx" from Clerk).
    /// Returns null if not authenticated.
    /// </summary>
    string? ExternalUserId { get; }

    /// <summary>
    /// User's email address from JWT claims.
    /// Returns null if not available.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Indicates whether the current request is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}

/// <summary>
/// Extended interface for services that need to initialize user context asynchronously.
/// Used internally by middleware to properly resolve user IDs without blocking.
/// </summary>
public interface ICurrentUserInitializer
{
    /// <summary>
    /// Initializes the user context by resolving external IDs to internal IDs.
    /// This should be called once per request by middleware.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicates whether the user context has been initialized.
    /// </summary>
    bool IsInitialized { get; }
}
