using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Errors;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Organizations.Commands.DeleteOrganization;

public sealed class DeleteOrganizationHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    ICacheService cache) : ICommandHandler<DeleteOrganizationCommand>
{
    public async Task<Result> Handle(
        DeleteOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.Id, cancellationToken);

        if (org is null)
            return OrganizationErrors.NotFound;

        if (org.ExternalId is not null)
        {
            var clerkResult = await clerkService.DeleteOrganizationAsync(
                org.ExternalId, cancellationToken);
            if (clerkResult.IsFailure)
                return OrganizationErrors.SyncFailed;
        }

        await cache.RemoveByPatternAsync($"org:{org.Id}:*", cancellationToken);

        dbContext.Organizations.Remove(org);

        Log.Information("Hard deleted organization {PublicId} ({Name})",
            org.PublicId, org.Name);

        return Result.Success();
    }
}
