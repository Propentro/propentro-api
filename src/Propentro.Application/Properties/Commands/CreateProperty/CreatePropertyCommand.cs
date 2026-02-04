
namespace Propentro.Application.Properties.Commands.CreateProperty;

public sealed record CreatePropertyCommand(
    string Name,
    string PropertyKind,
    Guid? AddressId,
    Guid? ParentPropertyId,
    int? YearBuilt,
    decimal? TotalAreaSqm,
    int? TotalFloors,
    string? ReferenceCode,
    string? Note
) : ICommand<Guid>;
