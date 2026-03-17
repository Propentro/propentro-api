using Propentro.Application.Common.Models;
using Propentro.Contracts.Organizations.Invites;

namespace Propentro.Application.Common.Interfaces;

public interface IInviteService
{
    Task<Result<InviteResult>> InviteUserToOrganizationAsync(
        Organization org,
        string email,
        string rolePublicId,
        Guid invitedByUserId,
        CancellationToken cancellationToken);
}
