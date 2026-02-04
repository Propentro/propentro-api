
namespace Propentro.Application.Properties.Commands.UpdateProperty;

public sealed record UpdatePropertyCommand(
    string PublicId,
    string? Name,
    string? PropertyKind,
    Guid? AddressId,
    int? YearBuilt,
    decimal? TotalAreaSqm,
    int? TotalFloors,
    string? ReferenceCode,
    string? Note
) : ICommand;
