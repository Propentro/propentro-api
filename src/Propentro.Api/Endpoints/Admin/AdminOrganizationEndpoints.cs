using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Features.Admin.Organizations.Commands.CreateOrganization;
using Propentro.Application.Features.Admin.Organizations.Commands.DeactivateOrganization;
using Propentro.Application.Features.Admin.Organizations.Commands.DeleteOrganization;
using Propentro.Application.Features.Admin.Organizations.Commands.ReactivateOrganization;
using Propentro.Application.Features.Admin.Organizations.Commands.UpdateOrganization;
using Propentro.Application.Features.Admin.Organizations.Queries.GetOrganization;
using Propentro.Application.Features.Admin.Organizations.Queries.GetOrganizations;
using Propentro.Contracts.Admin.Organizations;
using Propentro.Application.Common.Errors;
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

        group.MapGet("/{id}", GetAsync)
            .WithName("AdminGetOrganization")
            .WithSummary("Get organization detail");

        group.MapPatch("/{id}", UpdateAsync)
            .WithName("AdminUpdateOrganization")
            .WithSummary("Update organization");

        group.MapPatch("/{id}/deactivate", DeactivateAsync)
            .WithName("AdminDeactivateOrganization")
            .WithSummary("Deactivate organization");

        group.MapPatch("/{id}/reactivate", ReactivateAsync)
            .WithName("AdminReactivateOrganization")
            .WithSummary("Reactivate organization");

        group.MapDelete("/{id}", DeleteAsync)
            .WithName("AdminDeleteOrganization")
            .WithSummary("Hard delete organization (irreversible)");
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
            request.Name, request.Slug,
            request.CurrencyCode, request.Timezone);
        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAsync(
        string id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetOrganizationQuery(id), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdateAsync(
        string id, UpdateOrganizationRequest request,
        ISender sender, CancellationToken ct)
    {
        var command = new UpdateOrganizationCommand(
            id, request.Name, request.ContactName, request.ContactEmail,
            request.ContactPhone, request.CurrencyCode, request.Timezone);
        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateAsync(
        string id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeactivateOrganizationCommand(id), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ReactivateAsync(
        string id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new ReactivateOrganizationCommand(id), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteAsync(
        string id, HttpContext httpContext, ISender sender, CancellationToken ct)
    {
        var confirmHeader = httpContext.Request.Headers["X-Confirm-Delete"].FirstOrDefault();
        if (string.IsNullOrEmpty(confirmHeader) || confirmHeader != id)
        {
            return ConfirmationErrors.DeleteNotConfirmed.ToHttpResult();
        }

        var result = await sender.Send(new DeleteOrganizationCommand(id), ct);
        return result.ToHttpResult();
    }
}
