using Propentro.Contracts.Addresses;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints;

public static class AddressEndpoints
{
    public static void MapAddressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/addresses")
            .WithTags("Addresses")
            .RequireAuthorization();

        group.MapGet("/", GetAddresses)
            .WithName("GetAddresses")
            .WithSummary("Get all addresses")
            .WithMetadata(new RequirePermissionAttribute("addresses:read"))
            .Produces<IEnumerable<AddressDto>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{publicId}", GetAddressById)
            .WithName("GetAddressById")
            .WithSummary("Get an address by public ID")
            .WithMetadata(new RequirePermissionAttribute("addresses:read"))
            .Produces<AddressDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/", CreateAddress)
            .WithName("CreateAddress")
            .WithSummary("Create a new address")
            .WithMetadata(new RequirePermissionAttribute("addresses:create"))
            .Produces<string>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPut("/{publicId}", UpdateAddress)
            .WithName("UpdateAddress")
            .WithSummary("Update an address")
            .WithMetadata(new RequirePermissionAttribute("addresses:update"))
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapDelete("/{publicId}", DeleteAddress)
            .WithName("DeleteAddress")
            .WithSummary("Delete an address")
            .WithMetadata(new RequirePermissionAttribute("addresses:delete"))
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    // TODO: Implement GetAddressesQuery in Application/Addresses/Queries/GetAddresses/
    private static Task<IResult> GetAddresses(CancellationToken ct)
    {
        return Task.FromResult(Results.StatusCode(StatusCodes.Status501NotImplemented));
    }

    // TODO: Implement GetAddressByIdQuery in Application/Addresses/Queries/GetAddressById/
    private static Task<IResult> GetAddressById(string publicId, CancellationToken ct)
    {
        return Task.FromResult(Results.StatusCode(StatusCodes.Status501NotImplemented));
    }

    // TODO: Implement CreateAddressCommand in Application/Addresses/Commands/CreateAddress/
    private static Task<IResult> CreateAddress(CreateAddressRequest request, CancellationToken ct)
    {
        return Task.FromResult(Results.StatusCode(StatusCodes.Status501NotImplemented));
    }

    // TODO: Implement UpdateAddressCommand in Application/Addresses/Commands/UpdateAddress/
    private static Task<IResult> UpdateAddress(string publicId, UpdateAddressRequest request, CancellationToken ct)
    {
        return Task.FromResult(Results.StatusCode(StatusCodes.Status501NotImplemented));
    }

    // TODO: Implement DeleteAddressCommand in Application/Addresses/Commands/DeleteAddress/
    private static Task<IResult> DeleteAddress(string publicId, CancellationToken ct)
    {
        return Task.FromResult(Results.StatusCode(StatusCodes.Status501NotImplemented));
    }
}
