using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Me;

public static class MeErrors
{
    public static readonly Error UserNotFound = new(
        "Me.UserNotFound",
        "Authenticated user was not found in the system.",
        ErrorType.NotFound);
}
