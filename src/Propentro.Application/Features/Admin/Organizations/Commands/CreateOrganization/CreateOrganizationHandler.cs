using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Organizations;
using Propentro.Domain.Common;

namespace Propentro.Application.Features.Admin.Organizations.Commands.CreateOrganization;

public sealed class CreateOrganizationHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService) : ICommandHandler<CreateOrganizationCommand, AdminOrganizationResponse>
{
    public async Task<Result<AdminOrganizationResponse>> Handle(
        CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var slugExists = await dbContext.Organizations
            .IgnoreQueryFilters()
            .AnyAsync(o => o.Slug == request.Slug, cancellationToken);

        if (slugExists)
            return AdminOrganizationErrors.SlugTaken;

        var org = new Organization
        {
            Id = Guid.CreateVersion7(),
            PublicId = PublicIdGenerator.Generate(Organization.PublicIdPrefix),
            Name = request.Name,
            Slug = request.Slug,
            AuthProvider = "clerk",
            ContactName = request.ContactName,
            ContactEmail = request.ContactEmail,
            CurrencyCode = request.CurrencyCode,
            Timezone = request.Timezone,
            SubscriptionPlan = request.SubscriptionPlan,
            IsActive = true
        };

        dbContext.Organizations.Add(org);

        // Sync to Clerk — if Clerk fails, UnitOfWorkBehavior rolls back DB
        var clerkResult = await clerkService.CreateOrganizationAsync(
            request.Name, request.Slug, cancellationToken);

        if (clerkResult.IsFailure)
            return AdminOrganizationErrors.SyncFailed;

        org.ExternalId = clerkResult.Value;

        Log.Information("Created organization {PublicId} ({Name})", org.PublicId, org.Name);

        return new AdminOrganizationResponse
        {
            OrganizationId = org.PublicId,
            Name = org.Name,
            Slug = org.Slug,
            ContactName = org.ContactName,
            ContactEmail = org.ContactEmail,
            CurrencyCode = org.CurrencyCode,
            Timezone = org.Timezone,
            SubscriptionPlan = org.SubscriptionPlan,
            IsActive = org.IsActive,
            MemberCount = 0,
            CreatedAt = org.CreatedAt
        };
    }
}
