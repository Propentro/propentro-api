using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Features.Organizations.Settings.Commands.UpdateOrganizationSettings;
using Propentro.Application.Features.Organizations.Settings.Queries.GetOrganizationSettings;
using Propentro.Contracts.Organizations.Settings;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints.Organizations;

internal static class SettingsEndpoints
{
    public static void MapOrganizationSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/organizations")
            .WithTags("Organizations Settings")
            .RequireAuthorization();

        group.MapGet("/settings", GetSettingsAsync)
            .WithName("GetOrganizationSettings")
            .WithSummary("Get organization settings")
            .WithMetadata(new RequirePermissionAttribute("org:settings:read"))
            .Produces<OrganizationSettingsResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/settings", UpdateSettingsAsync)
            .WithName("UpdateOrganizationSettings")
            .WithSummary("Update organization settings")
            .WithMetadata(new RequirePermissionAttribute("org:settings:update"))
            .Produces<OrganizationSettingsResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetSettingsAsync(
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetOrganizationSettingsQuery(), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdateSettingsAsync(
        UpdateOrganizationSettingsRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new UpdateOrganizationSettingsCommand(
            request.Name,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.Region,
            request.PostalCode,
            request.CountryCode,
            request.LegalName,
            request.TaxNumber,
            request.TaxCountry,
            request.CurrencyCode,
            request.Timezone);

        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }
}
