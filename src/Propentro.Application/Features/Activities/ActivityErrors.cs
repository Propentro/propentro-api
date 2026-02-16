using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Activities;

public static class ActivityErrors
{
    public static readonly Error NotFound = new(
        "Activity.NotFound",
        "Activity not found.",
        ErrorType.NotFound);

    public static Error NotFoundById(string id) => new(
        "Activity.NotFound",
        $"Activity with ID '{id}' was not found.",
        ErrorType.NotFound);

    public static readonly Error ContactNotFound = new(
        "Activity.ContactNotFound",
        "The specified contact was not found.",
        ErrorType.NotFound);

    public static readonly Error SystemActivityImmutable = new(
        "Activity.SystemActivityImmutable",
        "System activities cannot be modified.",
        ErrorType.Conflict);

    public static readonly Error InvalidActivityType = new(
        "Activity.InvalidActivityType",
        "Activity type must be 'note' or 'phone_call'.",
        ErrorType.Validation);
}
