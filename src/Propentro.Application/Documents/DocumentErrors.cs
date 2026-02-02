using Propentro.Application.Common.Models;

namespace Propentro.Application.Documents;

public static class DocumentErrors
{
    public static readonly Error NotFound = new(
        "DOCUMENT_NOT_FOUND",
        "Document was not found.",
        ErrorType.NotFound);

    public static Error NotFoundById(string publicId) => new(
        "DOCUMENT_NOT_FOUND",
        $"Document with ID '{publicId}' was not found.",
        ErrorType.NotFound,
        new { PublicId = publicId });

    public static readonly Error InvalidMimeType = new(
        "INVALID_MIME_TYPE",
        "The file type is not supported.",
        ErrorType.Validation);

    public static readonly Error FileTooLarge = new(
        "FILE_TOO_LARGE",
        "The file exceeds the maximum allowed size.",
        ErrorType.Validation);
}
