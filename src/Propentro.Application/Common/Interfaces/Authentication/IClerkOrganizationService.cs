using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Interfaces.Authentication;

public interface IClerkOrganizationService
{
    Task<Result> UpdateOrganizationAsync(string externalId, string name, CancellationToken cancellationToken);
    Task<Result> InviteMemberAsync(string orgExternalId, string email, string clerkRole, CancellationToken cancellationToken);
    Task<Result> AddMemberAsync(string orgExternalId, string userExternalId, string clerkRole, CancellationToken cancellationToken);
    Task<Result> RemoveMemberAsync(string orgExternalId, string userExternalId, CancellationToken cancellationToken);
}
