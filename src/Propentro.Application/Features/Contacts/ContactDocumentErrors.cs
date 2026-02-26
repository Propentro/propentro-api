using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Contacts;

public static class ContactDocumentErrors
{
    public static readonly Error DocumentNotFound = new(
        "ContactDocument.NotFound",
        "Document not found.",
        ErrorType.NotFound);

    public static Error DocumentNotFoundById(string id) => new(
        "ContactDocument.NotFound",
        $"Document with ID '{id}' was not found.",
        ErrorType.NotFound);

    public static readonly Error DocumentNotLinked = new(
        "ContactDocument.NotLinked",
        "Document is not linked to this contact.",
        ErrorType.NotFound);

    public static readonly Error FileTooLarge = new(
        "ContactDocument.FileTooLarge",
        "File size exceeds the maximum allowed limit of 10 MB.",
        ErrorType.Validation);

    public static readonly Error UnsupportedFileType = new(
        "ContactDocument.UnsupportedFileType",
        "The uploaded file type is not supported.",
        ErrorType.Validation);
}
