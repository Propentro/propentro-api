using Propentro.Application.Common.Models;

namespace Propentro.Application.Me;

public static class MeErrors
{
    public static readonly Error NotAuthenticated = new(
        "ME_NOT_AUTHENTICATED",
        "User is not authenticated.",
        ErrorType.Unauthorized);

    public static readonly Error UserNotFound = new(
        "ME_USER_NOT_FOUND",
        "Authenticated user was not found in the system.",
        ErrorType.NotFound);
}
