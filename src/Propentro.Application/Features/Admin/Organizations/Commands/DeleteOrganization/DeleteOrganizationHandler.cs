using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Organizations.Commands.DeleteOrganization;

public sealed class DeleteOrganizationHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    IPermissionService permissionService) : ICommandHandler<DeleteOrganizationCommand>
{
    public async Task<Result> Handle(
        DeleteOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.Id, cancellationToken);

        if (org is null)
            return AdminOrganizationErrors.NotFound;

        if (org.ExternalId is not null)
        {
            var clerkResult = await clerkService.DeleteOrganizationAsync(
                org.ExternalId, cancellationToken);
            if (clerkResult.IsFailure)
                return AdminOrganizationErrors.ClerkSyncFailed;
        }

        await permissionService.InvalidateOrganizationPermissionsCacheAsync(
            org.Id, cancellationToken);

        dbContext.Organizations.Remove(org);

        Log.Information("Hard deleted organization {PublicId} ({Name})",
            org.PublicId, org.Name);

        return Result.Success();
    }
}
