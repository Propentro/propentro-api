using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Features.Admin.Organizations.Commands.CreateOrganization;
using Propentro.Application.Features.Admin.Organizations.Commands.DeactivateOrganization;
using Propentro.Application.Features.Admin.Organizations.Commands.UpdateOrganization;
using Propentro.Application.Features.Admin.Organizations.Queries.GetOrganization;
using Propentro.Application.Features.Admin.Organizations.Queries.GetOrganizations;
using Propentro.Contracts.Admin.Organizations;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints.Admin;

internal static class AdminOrganizationEndpoints
{
    public static void MapAdminOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin/organizations")
            .WithTags("Admin Organizations")
            .RequireAuthorization()
            .WithMetadata(new RequireSystemAdministratorAttribute());

        group.MapGet("/", ListAsync)
            .WithName("AdminListOrganizations")
            .WithSummary("List all organizations");

        group.MapPost("/", CreateAsync)
            .WithName("AdminCreateOrganization")
            .WithSummary("Create a new organization");

        group.MapGet("/{publicId}", GetAsync)
            .WithName("AdminGetOrganization")
            .WithSummary("Get organization detail");

        group.MapPut("/{publicId}", UpdateAsync)
            .WithName("AdminUpdateOrganization")
            .WithSummary("Update organization");

        group.MapDelete("/{publicId}", DeactivateAsync)
            .WithName("AdminDeactivateOrganization")
            .WithSummary("Deactivate organization");
    }

    private static async Task<IResult> ListAsync(
        ISender sender,
        string? search,
        bool? isActive,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetOrganizationsQuery(search, isActive, page, pageSize), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateAsync(
        CreateOrganizationRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreateOrganizationCommand(
            request.Name, request.Slug, request.ContactName, request.ContactEmail,
            request.CurrencyCode, request.Timezone, request.SubscriptionPlan);
        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAsync(
        string publicId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetOrganizationQuery(publicId), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdateAsync(
        string publicId, UpdateOrganizationRequest request,
        ISender sender, CancellationToken ct)
    {
        var command = new UpdateOrganizationCommand(
            publicId, request.Name, request.ContactName, request.ContactEmail,
            request.ContactPhone, request.CurrencyCode, request.Timezone);
        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateAsync(
        string publicId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeactivateOrganizationCommand(publicId), ct);
        return result.ToHttpResult();
    }
}
