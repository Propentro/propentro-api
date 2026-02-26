namespace Propentro.Application.Features.Contacts.Commands.UpdateContactDocument;

public record UpdateContactDocumentCommand(
    string ContactId,
    string DocumentId,
    string? Name,
    string? Description,
    bool? IsPrimary) : ICommand;
