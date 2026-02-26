using Propentro.Contracts.Documents;

namespace Propentro.Application.Features.Contacts.Commands.InitiateContactDocumentVersionUpload;

public record InitiateContactDocumentVersionUploadCommand(
    string ContactId,
    string DocumentId,
    string FileName,
    string ContentType,
    long FileSize) : ICommand<ContactDocumentUploadResponse>;
