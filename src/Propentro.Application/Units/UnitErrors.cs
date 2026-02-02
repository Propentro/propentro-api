using Propentro.Application.Common.Models;

namespace Propentro.Application.Units;

public static class UnitErrors
{
    public static readonly Error NotFound = new(
        "UNIT_NOT_FOUND",
        "Unit was not found.",
        ErrorType.NotFound);

    public static Error NotFoundById(string publicId) => new(
        "UNIT_NOT_FOUND",
        $"Unit with ID '{publicId}' was not found.",
        ErrorType.NotFound,
        new { PublicId = publicId });

    public static readonly Error PropertyNotFound = new(
        "UNIT_PROPERTY_NOT_FOUND",
        "Property for this unit was not found.",
        ErrorType.Validation);

    public static readonly Error CannotDeleteWithActiveLeases = new(
        "UNIT_HAS_ACTIVE_LEASES",
        "Cannot delete unit with active leases.",
        ErrorType.Conflict);
}
