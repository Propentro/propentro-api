using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Errors;

public static class ConfirmationErrors
{
    public static readonly Error DeleteNotConfirmed = Error.Validation(
        "Confirmation.DeleteNotConfirmed",
        "X-Confirm-Delete header must match the resource ID.");
}
