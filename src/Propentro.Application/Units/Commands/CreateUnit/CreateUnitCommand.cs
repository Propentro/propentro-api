
namespace Propentro.Application.Units.Commands.CreateUnit;

public sealed record CreateUnitCommand(
    Guid PropertyId,
    string Name,
    string UnitType,
    string? UsageType,
    string? RentalMode,
    int? Floor,
    string? UnitNumber,
    int? Bedrooms,
    decimal? Bathrooms,
    decimal? AreaSqm,
    long? DefaultRentMinor,
    long? DefaultDepositMinor,
    string? DefaultCurrency,
    string? Note
) : ICommand<string>;
