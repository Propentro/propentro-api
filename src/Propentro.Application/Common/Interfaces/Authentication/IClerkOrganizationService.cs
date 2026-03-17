using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Interfaces.Authentication;

public interface IClerkOrganizationService
{
    Task<Result> UpdateOrganizationAsync(string externalId, string name, CancellationToken cancellationToken);
    Task<Result> InviteMemberAsync(string orgExternalId, string email, string clerkRole, CancellationToken cancellationToken);
    Task<Result> AddMemberAsync(string orgExternalId, string userExternalId, string clerkRole, CancellationToken cancellationToken);
    Task<Result> RemoveMemberAsync(string orgExternalId, string userExternalId, CancellationToken cancellationToken);
    Task<Result<string>> CreateOrganizationAsync(string name, string slug, CancellationToken cancellationToken = default);
    Task<Result> DeleteOrganizationAsync(string externalId, CancellationToken cancellationToken = default);
    Task<Result> RevokeInvitationAsync(string externalOrgId, string email, CancellationToken cancellationToken = default);
    Task<Result> DeleteUserAsync(string userExternalId, CancellationToken cancellationToken = default);
}
