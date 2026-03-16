namespace Propentro.Application.Features.Admin.Users.Commands.RemoveMember;

public sealed record RemoveMemberCommand(string OrganizationId, string UserId) : ICommand;
