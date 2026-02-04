
namespace Propentro.Application.Units.Commands.UpdateUnit;

public sealed record UpdateUnitCommand(
    string PublicId,
    string? Name,
    string? UnitType,
    string? UsageType,
    string? RentalMode,
    string? Status,
    int? Floor,
    string? UnitNumber,
    int? Bedrooms,
    decimal? Bathrooms,
    decimal? AreaSqm,
    long? DefaultRentMinor,
    long? DefaultDepositMinor,
    string? DefaultCurrency,
    string? Note
) : ICommand;
