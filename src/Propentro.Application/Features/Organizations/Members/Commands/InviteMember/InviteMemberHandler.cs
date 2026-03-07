using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Organizations.Members.Commands.InviteMember;

public sealed class InviteMemberHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IClerkOrganizationService clerkService) : ICommandHandler<InviteMemberCommand>
{
    public async Task<Result> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == currentUser.OrganizationId, cancellationToken);

        if (org is null)
            return MemberErrors.NotFound;

        if (string.IsNullOrEmpty(org.ExternalId))
        {
            Log.Warning("Organization {OrganizationId} has no Clerk ExternalId — skipping invitation", org.PublicId);
            return Result.Success();
        }

        string clerkRole = "org:organization_viewer"; // Default role

        if (!string.IsNullOrWhiteSpace(request.RoleId))
        {
            var role = await dbContext.Roles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.PublicId == request.RoleId
                    && r.IsActive
                    && (r.IsSystem || r.OrganizationId == currentUser.OrganizationId),
                    cancellationToken);

            if (role is null)
                return MemberErrors.InvalidRoleIds;

            clerkRole = MapToClerkRole(role.Name);
        }

        return await clerkService.InviteMemberAsync(org.ExternalId, request.Email, clerkRole, cancellationToken);
    }

    // TODO: This mapping is a bit brittle, consider a more robust solution if more custom roles are added in the future
    private static string MapToClerkRole(string roleName) => roleName switch
    {
        "Owner" => "org:organization_owner",
        "Manager" => "org:organization_manager",
        _ => "org:organization_viewer"
    };
}
