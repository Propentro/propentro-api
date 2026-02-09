using Propentro.Application.Common.Models;

namespace Propentro.Application.MyProfile;

public static class MeErrors
{
    public static readonly Error UserNotFound = new(
        "MyProfile.UserNotFound",
        "Authenticated user was not found in the system.",
        ErrorType.NotFound);
}
