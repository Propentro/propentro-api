using Propentro.Application.Common.Models;

namespace Propentro.Application.Addresses;

public static class AddressErrors
{
    public static readonly Error NotFound = new(
        "ADDRESS_NOT_FOUND",
        "Address was not found.",
        ErrorType.NotFound);

    public static Error NotFoundById(string publicId) => new(
        "ADDRESS_NOT_FOUND",
        $"Address with ID '{publicId}' was not found.",
        ErrorType.NotFound,
        new { PublicId = publicId });

    public static readonly Error InvalidCountryCode = new(
        "INVALID_COUNTRY_CODE",
        "The country code is not valid. Must be a 2-letter ISO code.",
        ErrorType.Validation);
}
