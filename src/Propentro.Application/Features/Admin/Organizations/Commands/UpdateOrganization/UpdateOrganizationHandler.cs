using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Organizations;

namespace Propentro.Application.Features.Admin.Organizations.Commands.UpdateOrganization;

public sealed class UpdateOrganizationHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService) : ICommandHandler<UpdateOrganizationCommand, AdminOrganizationResponse>
{
    public async Task<Result<AdminOrganizationResponse>> Handle(
        UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.Id, cancellationToken);

        if (org is null || !org.IsActive)
            return AdminOrganizationErrors.NotFound;

        var nameChanged = org.Name != request.Name;

        org.Name = request.Name;
        org.ContactName = request.ContactName;
        org.ContactEmail = request.ContactEmail;
        org.ContactPhone = request.ContactPhone;
        org.CurrencyCode = request.CurrencyCode;
        org.Timezone = request.Timezone;

        // Sync name change to Clerk if org has an external ID
        if (nameChanged && org.ExternalId is not null)
        {
            var clerkResult = await clerkService.UpdateOrganizationAsync(
                org.ExternalId, request.Name, cancellationToken);

            if (clerkResult.IsFailure)
                return AdminOrganizationErrors.SyncFailed;
        }

        var memberCount = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .CountAsync(ou => ou.OrganizationId == org.Id, cancellationToken);

        Log.Information("Updated organization {PublicId} ({Name})", org.PublicId, org.Name);

        return new AdminOrganizationResponse
        {
            OrganizationId = org.PublicId,
            Name = org.Name,
            Slug = org.Slug,
            ContactName = org.ContactName,
            ContactEmail = org.ContactEmail,
            ContactPhone = org.ContactPhone,
            CurrencyCode = org.CurrencyCode,
            Timezone = org.Timezone,
            SubscriptionPlan = org.SubscriptionPlan,
            IsActive = org.IsActive,
            MemberCount = memberCount,
            CreatedAt = org.CreatedAt
        };
    }
}
