using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Errors;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Organizations.Commands.ReactivateOrganization;

public sealed class ReactivateOrganizationHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    ICacheService cache) : ICommandHandler<ReactivateOrganizationCommand>
{
    public async Task<Result> Handle(
        ReactivateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.Id, cancellationToken);

        if (org is null)
            return OrganizationErrors.NotFound;

        if (org.IsActive)
            return AdminOrganizationErrors.AlreadyActive;

        var clerkResult = await clerkService.CreateOrganizationAsync(
            org.Name, org.Slug, cancellationToken);

        if (clerkResult.IsFailure)
            return OrganizationErrors.SyncFailed;

        org.ExternalId = clerkResult.Value;
        org.IsActive = true;

        await cache.RemoveAsync($"org:{org.Id}:org-settings", cancellationToken);

        Log.Information("Reactivated organization {PublicId} ({Name})", org.PublicId, org.Name);

        return Result.Success();
    }
}
