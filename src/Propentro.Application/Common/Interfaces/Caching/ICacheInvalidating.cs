namespace Propentro.Application.Common.Interfaces.Caching;

/// <summary>
/// Marker interface for commands that should automatically invalidate cache entries
/// after successful transaction commit. Applied to MediatR commands and processed
/// by <see cref="Behaviors.UnitOfWorkBehavior{TRequest,TResponse}"/>.
/// </summary>
public interface ICacheInvalidating
{
    /// <summary>
    /// Cache keys to invalidate after successful commit.
    /// For org-scoped commands, provide bare key names (e.g., "organization-settings")
    /// and the behavior will add the <c>org:{OrganizationId}:</c> prefix automatically.
    /// For admin commands, provide fully-qualified keys and set <see cref="KeysAreFullyQualified"/> to true.
    /// </summary>
    IReadOnlyList<string> CacheKeysToInvalidate { get; }

    /// <summary>
    /// When true, cache keys are used as-is without org prefix.
    /// Default is false — the behavior adds <c>org:{OrganizationId}:</c> prefix to each key.
    /// </summary>
    bool KeysAreFullyQualified => false;
}
