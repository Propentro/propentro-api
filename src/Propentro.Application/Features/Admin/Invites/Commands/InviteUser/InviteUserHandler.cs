using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Errors;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Organizations.Invites;

namespace Propentro.Application.Features.Admin.Invites.Commands.InviteUser;

public sealed class InviteUserHandler(
    IAppDbContext dbContext,
    IInviteService inviteService) : ICommandHandler<InviteUserCommand, InviteResult>
{
    public async Task<Result<InviteResult>> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.OrganizationId && o.IsActive, cancellationToken);

        if (org is null)
            return InviteErrors.OrganizationNotFound;

        return await inviteService.InviteUserToOrganizationAsync(
            org,
            request.Email,
            request.RoleId,
            request.InvitedByUserId,
            cancellationToken);
    }
}
