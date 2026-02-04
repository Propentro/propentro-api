namespace Propentro.Application.Common.Interfaces;

public interface IClerkIdResolver
{
    Task<Guid> ResolveOrganizationIdAsync(string clerkOrgId, CancellationToken cancellationToken = default);
    Task<Guid> ResolveUserIdAsync(string clerkUserId, CancellationToken cancellationToken = default);
    Task<string> ResolvePublicUserIdAsync(string clerkUserId, CancellationToken cancellationToken = default);
}
