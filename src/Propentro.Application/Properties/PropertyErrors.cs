using Propentro.Application.Common.Models;

namespace Propentro.Application.Properties;

public static class PropertyErrors
{
    public static readonly Error NotFound = new(
        "PROPERTY_NOT_FOUND",
        "Property was not found.",
        ErrorType.NotFound);

    public static Error NotFoundById(string publicId) => new(
        "PROPERTY_NOT_FOUND",
        $"Property with ID '{publicId}' was not found.",
        ErrorType.NotFound,
        new { PublicId = publicId });

    public static readonly Error NameAlreadyExists = new(
        "PROPERTY_NAME_EXISTS",
        "A property with this name already exists.",
        ErrorType.Conflict);

    public static readonly Error CannotDeleteWithActiveLeases = new(
        "PROPERTY_HAS_ACTIVE_LEASES",
        "Cannot delete property with active leases.",
        ErrorType.Conflict);

    public static readonly Error InvalidParentProperty = new(
        "INVALID_PARENT_PROPERTY",
        "Parent property does not exist or belongs to another organization.",
        ErrorType.Validation);

    public static readonly Error ReferenceCodeExists = new(
        "PROPERTY_REFERENCE_EXISTS",
        "A property with this reference code already exists.",
        ErrorType.Conflict);
}
