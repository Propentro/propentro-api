using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Properties.Commands.CreateProperty;
using Propentro.Application.Properties.Commands.UpdateProperty;
using Propentro.Application.Properties.Commands.DeleteProperty;
using Propentro.Application.Properties.Queries.GetProperties;
using Propentro.Application.Properties.Queries.GetPropertyById;
using Propentro.Contracts.Properties;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints;

public static class PropertyEndpoints
{
    public static void MapPropertyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/properties")
            .WithTags("Properties")
            .RequireAuthorization();

        group.MapGet("/", GetProperties)
            .WithName("GetProperties")
            .WithSummary("Get all properties")
            .WithMetadata(new RequirePermissionAttribute("properties:read"))
            .Produces<PagedResult<PropertyDto>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{publicId}", GetPropertyById)
            .WithName("GetPropertyById")
            .WithSummary("Get a property by public ID")
            .WithMetadata(new RequirePermissionAttribute("properties:read"))
            .Produces<PropertyDetailDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/", CreateProperty)
            .WithName("CreateProperty")
            .WithSummary("Create a new property")
            .WithMetadata(new RequirePermissionAttribute("properties:create"))
            .Produces<string>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPut("/{publicId}", UpdateProperty)
            .WithName("UpdateProperty")
            .WithSummary("Update a property")
            .WithMetadata(new RequirePermissionAttribute("properties:update"))
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapDelete("/{publicId}", DeleteProperty)
            .WithName("DeleteProperty")
            .WithSummary("Delete a property")
            .WithMetadata(new RequirePermissionAttribute("properties:delete"))
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetProperties(
        ISender sender,
        [AsParameters] GetPropertiesQuery query,
        CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetPropertyById(
        string publicId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetPropertyByIdQuery(publicId), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateProperty(
        CreatePropertyRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreatePropertyCommand(
            request.Name,
            request.PropertyKind,
            request.AddressId,
            request.ParentPropertyId,
            request.YearBuilt,
            request.TotalAreaSqm,
            request.TotalFloors,
            request.ReferenceCode,
            request.Note);

        var result = await sender.Send(command, ct);
        return result.ToCreatedResult(id => $"/api/v1/properties/{id}");
    }

    private static async Task<IResult> UpdateProperty(
        string publicId,
        UpdatePropertyRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new UpdatePropertyCommand(
            publicId,
            request.Name,
            request.PropertyKind,
            request.AddressId,
            request.YearBuilt,
            request.TotalAreaSqm,
            request.TotalFloors,
            request.ReferenceCode,
            request.Note);

        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteProperty(
        string publicId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new DeletePropertyCommand(publicId), ct);
        return result.ToHttpResult();
    }
}

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
