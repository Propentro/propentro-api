using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Models;
using Serilog;

namespace Propentro.Application.Features.Webhooks.Commands.DeactivateOrganization;

/// <summary>
/// Soft-deletes an organization by (external_id, auth_provider). Idempotent.
/// Does NOT call SaveChangesAsync — UnitOfWorkBehavior handles persistence.
/// </summary>
public sealed class DeactivateOrganizationHandler(
    IAppDbContext dbContext) : ICommandHandler<DeactivateOrganizationCommand>
{
    public async Task<Result> Handle(DeactivateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                o => o.ExternalId == request.ExternalId && o.AuthProvider == request.AuthProvider,
                cancellationToken);

        if (org is null)
        {
            Log.Warning("Cannot deactivate organization: no org found for external ID {ExternalId}",
                request.ExternalId);
            return Result.Success();
        }

        if (!org.IsActive)
        {
            Log.Debug("Organization {PublicId} is already deactivated", org.PublicId);
            return Result.Success();
        }

        org.IsActive = false;

        Log.Information("Deactivated organization {PublicId} for external ID {ExternalId}",
            org.PublicId, request.ExternalId);

        return Result.Success();
    }
}
