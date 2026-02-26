namespace Propentro.Application.Features.Contacts.Commands.ConfirmContactDocumentUpload;

public record ConfirmContactDocumentUploadCommand(
    string ContactId,
    string DocumentId) : ICommand;
