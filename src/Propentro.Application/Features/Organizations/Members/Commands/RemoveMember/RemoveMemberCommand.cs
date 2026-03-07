namespace Propentro.Application.Features.Organizations.Members.Commands.RemoveMember;

public sealed record RemoveMemberCommand(string UserId) : ICommand;
