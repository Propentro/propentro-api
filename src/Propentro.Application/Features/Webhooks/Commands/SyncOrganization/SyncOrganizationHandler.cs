using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;
using Propentro.Domain.Common;
using Serilog;

namespace Propentro.Application.Features.Webhooks.Commands.SyncOrganization;

/// <summary>
/// Upserts an organization by (external_id, auth_provider). Idempotent.
/// On create, sets default currency (EUR) and timezone (UTC).
/// Does NOT call SaveChangesAsync — UnitOfWorkBehavior handles persistence.
/// </summary>
public sealed class SyncOrganizationHandler(IAppDbContext dbContext) : ICommandHandler<SyncOrganizationCommand>
{
    public async Task<Result> Handle(SyncOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                o => o.ExternalId == request.ExternalId && o.AuthProvider == request.AuthProvider,
                cancellationToken);

        if (org is null)
        {
            org = new Organization
            {
                Id = Guid.CreateVersion7(),
                PublicId = PublicIdGenerator.Generate(Organization.PublicIdPrefix),
                ExternalId = request.ExternalId,
                AuthProvider = request.AuthProvider,
                Name = request.Name,
                Slug = request.Slug,
                CurrencyCode = "EUR",
                Timezone = "UTC",
                IsActive = true
            };

            dbContext.Organizations.Add(org);
            Log.Information("Created organization {PublicId} for external ID {ExternalId}",
                org.PublicId, request.ExternalId);
        }
        else
        {
            if (!org.IsActive)
            {
                org.IsActive = true;
                Log.Warning(
                    "Reactivated previously deactivated organization {PublicId}. " +
                    "Memberships remain disabled — Clerk will re-sync them via membership events.",
                    org.PublicId);
            }

            org.Name = request.Name;
            org.Slug = request.Slug;

            Log.Information("Updated organization {PublicId} for external ID {ExternalId}",
                org.PublicId, request.ExternalId);
        }

        return Result.Success();
    }
}
