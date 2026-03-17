using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Organizations.Commands.ReactivateOrganization;

public sealed class ReactivateOrganizationHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService) : ICommandHandler<ReactivateOrganizationCommand>
{
    public async Task<Result> Handle(
        ReactivateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.Id, cancellationToken);

        if (org is null)
            return AdminOrganizationErrors.NotFound;

        if (org.IsActive)
            return AdminOrganizationErrors.AlreadyActive;

        // Re-create org in Clerk — deactivation deleted it, so we need a new Clerk entry
        var clerkResult = await clerkService.CreateOrganizationAsync(
            org.Name, org.Slug, cancellationToken);

        if (clerkResult.IsFailure)
            return AdminOrganizationErrors.ClerkSyncFailed;

        org.ExternalId = clerkResult.Value;
        org.IsActive = true;

        Log.Information("Reactivated organisation {PublicId} ({Name})", org.PublicId, org.Name);

        return Result.Success();
    }
}
