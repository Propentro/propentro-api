using Propentro.Contracts.Organizations;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints;

public static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/organization")
            .WithTags("Organization")
            .RequireAuthorization();

        group.MapGet("/", GetCurrentOrganization)
            .WithName("GetCurrentOrganization")
            .WithSummary("Get the current organization details")
            .WithMetadata(new RequirePermissionAttribute("organization:read"))
            .Produces<OrganizationDto>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPut("/", UpdateOrganization)
            .WithName("UpdateOrganization")
            .WithSummary("Update the current organization")
            .WithMetadata(new RequirePermissionAttribute("organization:update"))
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    // TODO: Implement GetOrganizationQuery in Application/Organizations/Queries/GetOrganization/
    private static Task<IResult> GetCurrentOrganization(CancellationToken ct)
    {
        return Task.FromResult(Results.StatusCode(StatusCodes.Status501NotImplemented));
    }

    // TODO: Implement UpdateOrganizationCommand in Application/Organizations/Commands/UpdateOrganization/
    private static Task<IResult> UpdateOrganization(UpdateOrganizationRequest request, CancellationToken ct)
    {
        return Task.FromResult(Results.StatusCode(StatusCodes.Status501NotImplemented));
    }
}
