namespace Propentro.Application.Features.Organizations.Members.Commands.ReactivateMember;

public sealed record ReactivateMemberCommand(string UserId) : ICommand;
