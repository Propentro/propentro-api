using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Errors;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Organizations;

namespace Propentro.Application.Features.Admin.Organizations.Commands.UpdateOrganization;

public sealed class UpdateOrganizationHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    ICacheService cache) : ICommandHandler<UpdateOrganizationCommand, AdminOrganizationResponse>
{
    public async Task<Result<AdminOrganizationResponse>> Handle(
        UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.Id, cancellationToken);

        if (org is null || !org.IsActive)
            return OrganizationErrors.NotFound;

        var nameChanged = org.Name != request.Name;

        org.Name = request.Name;
        org.Slug = request.Slug;
        org.ContactName = request.ContactName;
        org.ContactEmail = request.ContactEmail;
        org.ContactPhone = request.ContactPhone;
        org.CurrencyCode = request.CurrencyCode;
        org.Timezone = request.Timezone;
        org.AddressLine1 = request.AddressLine1;
        org.AddressLine2 = request.AddressLine2;
        org.City = request.City;
        org.Region = request.Region;
        org.PostalCode = request.PostalCode;
        org.CountryCode = request.CountryCode;
        org.LegalName = request.LegalName;
        org.TaxNumber = request.TaxNumber;
        org.TaxCountry = request.TaxCountry;

        if (nameChanged && org.ExternalId is not null)
        {
            var clerkResult = await clerkService.UpdateOrganizationAsync(
                org.ExternalId, request.Name, cancellationToken);

            if (clerkResult.IsFailure)
                return OrganizationErrors.SyncFailed;
        }

        await cache.RemoveAsync($"org:{org.Id}:org-settings", cancellationToken);

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
            AddressLine1 = org.AddressLine1,
            AddressLine2 = org.AddressLine2,
            City = org.City,
            Region = org.Region,
            PostalCode = org.PostalCode,
            CountryCode = org.CountryCode,
            LegalName = org.LegalName,
            TaxNumber = org.TaxNumber,
            TaxCountry = org.TaxCountry,
            IsActive = org.IsActive,
            MemberCount = memberCount,
            CreatedAt = org.CreatedAt
        };
    }
}
