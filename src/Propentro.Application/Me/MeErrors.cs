using Propentro.Application.Common.Models;

namespace Propentro.Application.Me;

public static class MeErrors
{
    public static readonly Error NotAuthenticated = new(
        "Me.NotAuthenticated",
        "User is not authenticated.",
        ErrorType.Unauthorized);

    public static readonly Error UserNotFound = new(
        "Me.UserNotFound",
        "Authenticated user was not found in the system.",
        ErrorType.NotFound);
}
