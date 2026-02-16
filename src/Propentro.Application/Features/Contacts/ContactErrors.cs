using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Contacts;

public static class ContactErrors
{
    public static readonly Error NotFound = new(
        "Contact.NotFound",
        "Contact not found.",
        ErrorType.NotFound);

    public static Error NotFoundById(string id) => new(
        "Contact.NotFound",
        $"Contact with ID '{id}' was not found.",
        ErrorType.NotFound);

    public static readonly Error AlreadyArchived = new(
        "Contact.AlreadyArchived",
        "Contact is already archived.",
        ErrorType.Conflict);

    public static readonly Error NotArchived = new(
        "Contact.NotArchived",
        "Contact is not archived.",
        ErrorType.Conflict);

    public static readonly Error InvalidRole = new(
        "Contact.InvalidRole",
        "One or more roles are invalid.",
        ErrorType.Validation);

    public static readonly Error HasActiveLeases = new(
        "Contact.HasActiveLeases",
        "Cannot delete contact with active leases.",
        ErrorType.Conflict);

    public static readonly Error DuplicateEmail = new(
        "Contact.DuplicateEmail",
        "A contact with this email already exists.",
        ErrorType.Conflict);
}
