namespace Propentro.Application.Features.Admin.Users.Commands.RemoveMember;

public sealed record RemoveMemberCommand(string OrgPublicId, string UserPublicId) : ICommand;
