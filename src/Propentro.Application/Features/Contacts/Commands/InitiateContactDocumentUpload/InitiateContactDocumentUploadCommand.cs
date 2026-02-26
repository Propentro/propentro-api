using Propentro.Contracts.Documents;

namespace Propentro.Application.Features.Contacts.Commands.InitiateContactDocumentUpload;

public record InitiateContactDocumentUploadCommand(
    string ContactId,
    string Name,
    string DocumentType,
    string? Description,
    bool IsPrimary,
    string FileName,
    string ContentType,
    long FileSize) : ICommand<ContactDocumentUploadResponse>;
