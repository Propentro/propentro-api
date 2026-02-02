using Propentro.Application.Common.Models;

namespace Propentro.Application.Parties;

public static class PartyErrors
{
    public static readonly Error NotFound = new(
        "PARTY_NOT_FOUND",
        "Party was not found.",
        ErrorType.NotFound);

    public static Error NotFoundById(string publicId) => new(
        "PARTY_NOT_FOUND",
        $"Party with ID '{publicId}' was not found.",
        ErrorType.NotFound,
        new { PublicId = publicId });

    public static readonly Error EmailAlreadyExists = new(
        "PARTY_EMAIL_EXISTS",
        "A party with this email already exists.",
        ErrorType.Conflict);

    public static readonly Error CannotDeleteWithActiveLeases = new(
        "PARTY_HAS_ACTIVE_LEASES",
        "Cannot delete party with active lease associations.",
        ErrorType.Conflict);
}
