namespace Propentro.Application.Features.Contacts.Commands.SetContactTags;

public record SetContactTagsCommand(string ContactId, string[] TagIds) : ICommand;
