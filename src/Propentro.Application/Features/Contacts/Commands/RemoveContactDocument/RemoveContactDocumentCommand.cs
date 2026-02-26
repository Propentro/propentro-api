namespace Propentro.Application.Features.Contacts.Commands.RemoveContactDocument;

public record RemoveContactDocumentCommand(
    string ContactId,
    string DocumentId) : ICommand;
