using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Errors;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Organizations.Settings;

namespace Propentro.Application.Features.Organizations.Settings.Commands.UpdateOrganizationSettings;

public sealed class UpdateOrganizationSettingsHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IClerkOrganizationService clerkOrganizationService,
    ICacheService cache) : ICommandHandler<UpdateOrganizationSettingsCommand, OrganizationSettingsResponse>
{
    public async Task<Result<OrganizationSettingsResponse>> Handle(UpdateOrganizationSettingsCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == currentUser.OrganizationId, cancellationToken);

        if (org is null)
            return OrganizationErrors.NotFound;

        var nameChanged = !string.Equals(org.Name, request.Name, StringComparison.Ordinal);

        // Sync name change to Clerk before committing to DB.
        // If Clerk is unavailable, roll back the entire operation.
        if (nameChanged && !string.IsNullOrEmpty(org.ExternalId))
        {
            var syncResult = await clerkOrganizationService.UpdateOrganizationAsync(
                org.ExternalId,
                request.Name,
                cancellationToken);

            if (syncResult.IsFailure)
            {
                Log.Warning(
                    "Failed to sync organization name to Clerk for {OrganizationId}: {Error}",
                    org.PublicId,
                    syncResult.Error.Message);

                return OrganizationErrors.SyncFailed;
            }
        }

        await cache.RemoveAsync($"org:{currentUser.OrganizationId}:org-settings", cancellationToken);

        org.Name = request.Name;
        org.ContactName = request.ContactName;
        org.ContactEmail = request.ContactEmail;
        org.ContactPhone = request.ContactPhone;
        org.AddressLine1 = request.AddressLine1;
        org.AddressLine2 = request.AddressLine2;
        org.City = request.City;
        org.Region = request.Region;
        org.PostalCode = request.PostalCode;
        org.CountryCode = request.CountryCode;
        org.LegalName = request.LegalName;
        org.TaxNumber = request.TaxNumber;
        org.TaxCountry = request.TaxCountry;
        org.CurrencyCode = request.CurrencyCode;
        org.Timezone = request.Timezone;

        return new OrganizationSettingsResponse
        {
            OrganizationId = org.PublicId,
            Name = org.Name,
            Slug = org.Slug,
            ContactName = org.ContactName,
            ContactEmail = org.ContactEmail,
            ContactPhone = org.ContactPhone,
            AddressLine1 = org.AddressLine1,
            AddressLine2 = org.AddressLine2,
            City = org.City,
            Region = org.Region,
            PostalCode = org.PostalCode,
            CountryCode = org.CountryCode,
            LegalName = org.LegalName,
            TaxNumber = org.TaxNumber,
            TaxCountry = org.TaxCountry,
            CurrencyCode = org.CurrencyCode,
            Timezone = org.Timezone
        };
    }
}
