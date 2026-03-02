using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Organizations;

namespace Propentro.Application.Features.Organizations.Commands.UpdateOrganizationSettings;

public sealed class UpdateOrganizationSettingsHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IClerkOrganizationService clerkOrganizationService) : ICommandHandler<UpdateOrganizationSettingsCommand, OrganizationSettingsResponse>
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

                return OrganizationErrors.ClerkSyncFailed;
            }
        }

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

        return new OrganizationSettingsResponse(
            org.PublicId,
            org.Name,
            org.Slug,
            org.ContactName,
            org.ContactEmail,
            org.ContactPhone,
            org.AddressLine1,
            org.AddressLine2,
            org.City,
            org.Region,
            org.PostalCode,
            org.CountryCode,
            org.LegalName,
            org.TaxNumber,
            org.TaxCountry,
            org.CurrencyCode,
            org.Timezone);
    }
}
