using Propentro.Application.Common.Models;

namespace Propentro.Application.Leases;

public static class LeaseErrors
{
    public static readonly Error NotFound = new(
        "LEASE_NOT_FOUND",
        "Lease was not found.",
        ErrorType.NotFound);

    public static Error NotFoundById(string publicId) => new(
        "LEASE_NOT_FOUND",
        $"Lease with ID '{publicId}' was not found.",
        ErrorType.NotFound,
        new { PublicId = publicId });

    public static readonly Error AlreadyActive = new(
        "LEASE_ALREADY_ACTIVE",
        "Lease is already active.",
        ErrorType.Conflict);

    public static readonly Error CannotRenew = new(
        "LEASE_CANNOT_RENEW",
        "Lease cannot be renewed in its current status.",
        ErrorType.Conflict);

    public static readonly Error UnitNotAvailable = new(
        "UNIT_NOT_AVAILABLE",
        "One or more units are not available for the specified dates.",
        ErrorType.Conflict);

    public static readonly Error InvalidDateRange = new(
        "INVALID_DATE_RANGE",
        "End date must be after start date.",
        ErrorType.Validation);

    public static readonly Error CannotCancel = new(
        "LEASE_CANNOT_CANCEL",
        "Active lease cannot be cancelled. End the lease instead.",
        ErrorType.Conflict);
}
