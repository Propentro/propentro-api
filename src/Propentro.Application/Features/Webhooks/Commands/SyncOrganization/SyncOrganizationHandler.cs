using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Messaging;
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
            org = new Domain.Entities.Organization
            {
                Id = Guid.CreateVersion7(),
                PublicId = PublicIdGenerator.Generate("org"),
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
            org.Name = request.Name;
            org.Slug = request.Slug;
            org.IsActive = true;

            Log.Information("Updated organization {PublicId} for external ID {ExternalId}",
                org.PublicId, request.ExternalId);
        }

        return Result.Success();
    }
}
