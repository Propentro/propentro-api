namespace Propentro.Application.Features.Contacts.Commands.AddContactDocument;

public record AddContactDocumentCommand(
    string ContactId,
    string Name,
    string DocumentType,
    string? Description,
    bool IsPrimary,
    Stream FileContent,
    string FileName,
    string ContentType,
    long FileSize) : ICommand<string>;
