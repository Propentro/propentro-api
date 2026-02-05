namespace Propentro.Application.Properties.Commands.CreateProperty;

/// <summary>
/// Command to create a new property.
/// Returns the public identifier (PublicId) of the created property.
/// </summary>
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
) : ICommand<string>;
