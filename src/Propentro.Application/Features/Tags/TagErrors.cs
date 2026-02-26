using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Tags;

public static class TagErrors
{
    public static readonly Error NotFound = new(
        "Tag.NotFound",
        "Tag not found.",
        ErrorType.NotFound);

    public static Error NotFoundById(string id) => new(
        "Tag.NotFound",
        $"Tag with ID '{id}' was not found.",
        ErrorType.NotFound);

    public static readonly Error DuplicateName = new(
        "Tag.DuplicateName",
        "A tag with this name already exists.",
        ErrorType.Conflict);

    public static readonly Error TagAlreadyAssigned = new(
        "Tag.AlreadyAssigned",
        "This tag is already assigned to the entity.",
        ErrorType.Conflict);

    public static readonly Error TagNotAssigned = new(
        "Tag.NotAssigned",
        "This tag is not assigned to the entity.",
        ErrorType.NotFound);
}
